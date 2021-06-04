using System;
using System.Collections.Generic;
using System.Diagnostics;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Iterative;
using ISAAR.MSolve.LinearAlgebra.Iterative.Termination;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Environments;
using MGroup.LinearAlgebra.Distributed.IterativeMethods;
using MGroup.LinearAlgebra.Distributed.Overlapping;
using MGroup.Solvers.DomainDecomposition.PSM.Dofs;
using MGroup.Solvers.DomainDecomposition.PSM.InterfaceProblem;
using MGroup.Solvers.DomainDecomposition.PSM.Preconditioning;
using MGroup.Solvers.DomainDecomposition.PSM.StiffnessDistribution;
using MGroup.Solvers.DomainDecomposition.PSM.StiffnessMatrices;
using MGroup.Solvers.DomainDecomposition.PSM.Vectors;
using MGroup.Solvers.DomainDecomposition.StiffnessMatrices;
using MGroup.Solvers.LinearSystems;
using MGroup.Solvers.Ordering;
using MGroup.Solvers.Ordering.Reordering;

namespace MGroup.Solvers.DomainDecomposition.Psm
{
	public class PsmSolver : ISolver
	{
		protected readonly IDofOrderer dofOrderer;
		protected readonly IPsmDofSeparator dofSeparatorPsm;
		protected readonly IComputeEnvironment environment;
		protected readonly IPsmInterfaceProblemMatrix interfaceProblemMatrix;
		protected readonly IDistributedIterativeMethod interfaceProblemSolver;
		protected readonly IMatrixManager matrixManagerBasic;
		protected readonly IPsmMatrixManager matrixManagerPsm;
		protected readonly IStructuralModel model;
		protected readonly string name;
		protected readonly IPsmPreconditioner preconditioner;
		protected readonly IPsmRhsVectorManager rhsVectorManager;
		protected readonly IPsmSolutionVectorManager solutionVectorManager;
		protected readonly IStiffnessDistribution stiffnessDistribution;
		protected readonly SubdomainTopology subdomainTopology;

		private DistributedOverlappingIndexer indexer; //TODOMPI: Perhaps this should be accessed from DofSeparator

		protected PsmSolver(IComputeEnvironment environment, IStructuralModel model, SubdomainTopology subdomainTopology,
			IDofOrderer dofOrderer, IPsmDofSeparator dofSeparator, IMatrixManager matrixManagerBasic,
			IPsmMatrixManager matrixManagerPsm, bool explicitSubdomainMatrices, IPsmPreconditioner preconditioner,
			IDistributedIterativeMethod interfaceProblemSolver, bool isHomogeneous, string name = "PSM Solver")
		{
			this.name = name;
			this.environment = environment;

			this.model = model;
			this.subdomainTopology = subdomainTopology;
			this.dofOrderer = dofOrderer;
			this.dofSeparatorPsm = dofSeparator;
			this.matrixManagerBasic = matrixManagerBasic;
			this.matrixManagerPsm = matrixManagerPsm;
			this.preconditioner = preconditioner;
			this.interfaceProblemSolver = interfaceProblemSolver;

			if (explicitSubdomainMatrices)
			{
				this.interfaceProblemMatrix = new PsmInterfaceProblemMatrixExplicit(environment, matrixManagerPsm);
			}
			else
			{
				this.interfaceProblemMatrix = new PsmInterfaceProblemMatrixImplicit(environment, dofSeparator, matrixManagerPsm);
			}

			if (isHomogeneous)
			{
				this.stiffnessDistribution = new HomogeneousStiffnessDistribution(environment, model, dofSeparator);
			}
			else
			{
				this.stiffnessDistribution = new HeterogeneousStiffnessDistribution(
					environment, model, dofSeparator, matrixManagerBasic);
			}

			Dictionary<int, ILinearSystem> linearSystems = environment.CreateDictionaryPerNode(
				subdomainID => matrixManagerBasic.GetLinearSystem(subdomainID));
			LinearSystems = linearSystems;

			this.rhsVectorManager = new PsmRhsVectorManager(environment, dofSeparator, linearSystems, matrixManagerPsm);
			this.solutionVectorManager = new PsmSolutionVectorManager(environment, dofSeparator,
				matrixManagerBasic, matrixManagerPsm, rhsVectorManager);

			Logger = new SolverLogger(name);
		}

		public IReadOnlyDictionary<int, ILinearSystem> LinearSystems { get; }

		public SolverLogger Logger { get; }

		public string Name => name;

		public bool StartIterativeSolverFromPreviousSolution { get; set; } = false;

		public virtual Dictionary<int, IMatrix> BuildGlobalMatrices(IElementMatrixProvider elementMatrixProvider, 
			Func<int, bool> mustUpdateSubdomain)
		{
			//TODOMPI: This must be called after ISolver.Initialize()
			Func<int, IMatrix> buildKff = subdomainID =>
			{
				Debug.WriteLine($"Subdomain {subdomainID} will try to build Kff");
				if (mustUpdateSubdomain(subdomainID))
				{
					ISubdomain subdomain = model.GetSubdomain(subdomainID);
					matrixManagerBasic.BuildKff(
						subdomainID, subdomain.FreeDofOrdering, subdomain.Elements, elementMatrixProvider);
				}
				return (IMatrix)LinearSystems[subdomainID].Matrix;
			};
			Dictionary<int, IMatrix> matricesKff = environment.CreateDictionaryPerNode(buildKff);
			stiffnessDistribution.CalcSubdomainScaling(indexer);

			return matricesKff;
		}

		public virtual Dictionary<int, (IMatrix matrixFreeFree, IMatrixView matrixFreeConstr, IMatrixView matrixConstrFree,
			IMatrixView matrixConstrConstr)> BuildGlobalSubmatrices(IElementMatrixProvider elementMatrixProvider)
		{
			throw new NotImplementedException();
		}

		public virtual Dictionary<int, SparseVector> DistributeNodalLoads(Table<INode, IDofType, double> nodalLoads)
			=> stiffnessDistribution.DistributeNodalLoads(nodalLoads);

		public virtual void HandleMatrixWillBeSet()
		{
		}

		public virtual void Initialize()
		{
			// Reordering the internal dofs is not done here, since subdomain Kff must be built first. 
			dofSeparatorPsm.SeparateSubdomainDofsIntoBoundaryInternal(); 
			dofSeparatorPsm.FindCommonDofsBetweenSubdomains();
			this.indexer = dofSeparatorPsm.CreateDistributedVectorIndexer();

			//TODOMPI: What should I log here? And where? There is not a central place for logs.
			//Logger.LogNumDofs("Global boundary dofs", dofSeparatorPsm.GetNumBoundaryDofsCluster(clusterID));
		}

		public virtual Dictionary<int, Matrix> InverseSystemMatrixTimesOtherMatrix(Dictionary<int, IMatrixView> otherMatrix)
		{
			throw new NotImplementedException();
		}

		public virtual void OrderDofs(bool alsoOrderConstrainedDofs)
		{
			environment.DoPerNode(subdomainID =>
			{
				ISubdomain subdomain = model.GetSubdomain(subdomainID);
				subdomain.FreeDofOrdering = dofOrderer.OrderFreeDofs(subdomain);
			});
		}

		public virtual void PreventFromOverwrittingSystemMatrices()
		{
		}

		public virtual void Solve()
		{
			Action<int> calcSubdomainMatrices = subdomainID =>
			{
				//TODO: This should only happen if the connectivity of the subdomain changes. 
				environment.DoPerNode(subdomainID => matrixManagerPsm.ReorderInternalDofs(subdomainID));

				//TODO: These should happen if the connectivity or stiffness of the subdomain changes
				matrixManagerPsm.ExtractKiiKbbKib(subdomainID);
				matrixManagerPsm.InvertKii(subdomainID);
			};
			environment.DoPerNode(calcSubdomainMatrices);

			interfaceProblemMatrix.Calculate(indexer);
			preconditioner.Calculate(environment, indexer, interfaceProblemMatrix);

			SolveInterfaceProblem();
		}

		protected void SolveInterfaceProblem()
		{
			rhsVectorManager.Clear();
			rhsVectorManager.CalcRhsVectors(indexer);

			bool initalGuessIsZero = !StartIterativeSolverFromPreviousSolution;
			if (!StartIterativeSolverFromPreviousSolution)
			{
				solutionVectorManager.Initialize(indexer);
			}
			IterativeStatistics stats = interfaceProblemSolver.Solve(
				interfaceProblemMatrix.Matrix, preconditioner.Preconditioner, rhsVectorManager.InterfaceProblemRhs, 
				solutionVectorManager.InterfaceProblemSolution, initalGuessIsZero);
			Logger.LogIterativeAlgorithm(stats.NumIterationsRequired, stats.ResidualNormRatioEstimation);
			Debug.WriteLine("Iterations for boundary problem = " + stats.NumIterationsRequired);

			solutionVectorManager.CalcSubdomainDisplacements();
			Logger.IncrementAnalysisStep();
		}

		public class Builder
		{
			private readonly IComputeEnvironment environment;

			public Builder(IComputeEnvironment environment)
			{
				DofOrderer = new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering(), true);
				ExplicitSubdomainMatrices = false;

				//TODO: perhaps use a custom convergence check like in FETI
				var pcgBuilder = new PcgAlgorithm.Builder();
				pcgBuilder.ResidualTolerance = 1E-6;
				pcgBuilder.MaxIterationsProvider = new FixedMaxIterationsProvider(100);
				InterfaceProblemSolver = pcgBuilder.Build();
				IsHomogeneousProblem = true;

				MatrixManagerFactory = new PsmMatrixManagerSymmetricCSparse.Factory();
				Preconditioner = new PsmPreconditionerIdentity();
				this.environment = environment;
			}

			public IDofOrderer DofOrderer { get; set; }

			public bool ExplicitSubdomainMatrices { get; set; }

			public IDistributedIterativeMethod InterfaceProblemSolver { get; set; }

			public bool IsHomogeneousProblem { get; set; }

			public IPsmMatrixManagerFactory MatrixManagerFactory { get; set; }

			public IPsmPreconditioner Preconditioner { get; set; }

			public PsmSolver BuildSolver(IStructuralModel model, SubdomainTopology subdomainTopology)
			{
				var dofSeparator = new PsmDofSeparator(environment, model, subdomainTopology);
				var (matrixManagerBasic, matrixManagerPsm) = MatrixManagerFactory.CreateMatrixManagers(model, dofSeparator);
				return new PsmSolver(environment, model, subdomainTopology, DofOrderer, dofSeparator,
					matrixManagerBasic, matrixManagerPsm, ExplicitSubdomainMatrices, Preconditioner, InterfaceProblemSolver, 
					IsHomogeneousProblem);
			}
		}
	}
}
