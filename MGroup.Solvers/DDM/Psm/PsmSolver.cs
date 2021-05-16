using System;
using System.Collections.Generic;
using System.Diagnostics;

using ISAAR.MSolve.LinearAlgebra.Iterative;
using ISAAR.MSolve.LinearAlgebra.Iterative.PreconditionedConjugateGradient;
using ISAAR.MSolve.LinearAlgebra.Iterative.Termination;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Solvers;
using ISAAR.MSolve.Solvers.LinearSystems;
using MGroup.Solvers.DDM.Environments;
using MGroup.Solvers.DDM.Mappings;
using MGroup.Solvers.DDM.Psm.Dofs;
using MGroup.Solvers.DDM.Psm.InterfaceProblem;
using MGroup.Solvers.DDM.Psm.Preconditioner;
using MGroup.Solvers.DDM.Psm.StiffnessDistribution;
using MGroup.Solvers.DDM.Psm.StiffnessMatrices;
using MGroup.Solvers.DDM.Psm.Vectors;
using MGroup.Solvers.DDM.StiffnessMatrices;
using ISAAR.MSolve.Solvers.Ordering;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Solvers.Ordering.Reordering;

namespace MGroup.Solvers.DDM.Psm
{
	public class PsmSolver : ISolver
	{
		protected const int clusterID = 0;

		protected readonly IDofOrderer dofOrderer;
		protected readonly IPsmDofSeparator dofSeparatorPsm;
		protected readonly IDdmEnvironment environment;
		protected readonly IInterfaceProblemSolver interfaceProblemSolver;
		protected readonly IMatrixManager matrixManagerBasic;
		protected readonly IPsmMatrixManager matrixManagerPsm;
		protected readonly IStructuralModel model;
		protected readonly string name;
		protected readonly IPsmPreconditioner preconditioner;
		protected readonly IPsmRhsVectorManager rhsVectorManager;
		protected readonly IPsmSolutionVectorManager solutionVectorManager;
		protected readonly IStiffnessDistribution stiffnessDistribution;

		protected PsmSolver(IDdmEnvironment environment, IStructuralModel model, IList<Cluster> clusters, IDofOrderer dofOrderer, 
			IPsmDofSeparator dofSeparator, IMatrixManager matrixManagerBasic, IPsmMatrixManager matrixManagerPsm, 
			IPsmPreconditioner preconditioner, IInterfaceProblemSolver interfaceProblemSolver, bool isHomogeneous, 
			string name = "PSM Solver")
		{
			this.name = name;
			this.environment = environment;
			if (clusters.Count > 1)
			{
				throw new NotImplementedException();
			}
			this.model = model;
			this.dofOrderer = dofOrderer;
			this.dofSeparatorPsm = dofSeparator;
			this.matrixManagerBasic = matrixManagerBasic;
			this.matrixManagerPsm = matrixManagerPsm;
			this.preconditioner = preconditioner;
			this.interfaceProblemSolver = interfaceProblemSolver;

			if (isHomogeneous)
			{
				this.stiffnessDistribution = new HomogeneousStiffnessDistribution(environment, clusters, dofSeparator);
			}
			else
			{
				this.stiffnessDistribution = new HeterogeneousStiffnessDistribution(
					environment, clusters, dofSeparator, matrixManagerBasic);
			}

			var linearSystems = new Dictionary<int, ILinearSystem>();
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				linearSystems[subdomain.ID] = matrixManagerBasic.GetLinearSystem(subdomain.ID);
			}
			LinearSystems = linearSystems;

			this.rhsVectorManager = new PsmRhsVectorManager(environment, model, linearSystems, dofSeparator, matrixManagerPsm);
			this.solutionVectorManager = new PsmSolutionVectorManager(environment, model, 
				linearSystems, dofSeparator, matrixManagerBasic, matrixManagerPsm, rhsVectorManager);

			Logger = new SolverLogger(name);
		}

		public IReadOnlyDictionary<int, ILinearSystem> LinearSystems { get; }

		public SolverLogger Logger { get; }

		public string Name => name;

		public bool StartIterativeSolverFromPreviousSolution { get; set; } = false;

		public virtual Dictionary<int, IMatrix> BuildGlobalMatrices(IElementMatrixProvider elementMatrixProvider)
		{
			var matricesKff = new Dictionary<int, IMatrix>();
			Action<ISubdomain> subdomainAction = sub =>
			{
				IMatrix Kff = matrixManagerBasic.BuildKff(sub.ID, sub.FreeDofOrdering, sub.Elements, elementMatrixProvider);
				lock (matricesKff) matricesKff[sub.ID] = Kff;
			};
			environment.ExecuteSubdomainAction(model.Subdomains, subdomainAction);

			Initialize();

			return matricesKff;
		}

		public virtual Dictionary<int, (IMatrix matrixFreeFree, IMatrixView matrixFreeConstr, IMatrixView matrixConstrFree, IMatrixView matrixConstrConstr)> BuildGlobalSubmatrices(IElementMatrixProvider elementMatrixProvider)
		{
			throw new NotImplementedException();
		}

		public virtual Dictionary<int, SparseVector> DistributeNodalLoads(Table<INode, IDofType, double> globalNodalLoads)
			=> stiffnessDistribution.DistributeNodalLoads(globalNodalLoads, model.Subdomains);

		public virtual Dictionary<int, Vector> DistributeGlobalForces(Vector globalForces)
		{
			var subdomainForces = new Dictionary<int, Vector>();
			Action<ISubdomain> subdomainAction = sub =>
			{
				int s = sub.ID;
				int[] subdomainToGlobalDofs = model.GlobalDofOrdering.MapFreeDofsSubdomainToGlobal(sub);
				var forces = Vector.CreateZero(subdomainToGlobalDofs.Length);
				for (int i = 0; i < subdomainToGlobalDofs.Length; ++i)
				{
					forces[i] = globalForces[subdomainToGlobalDofs[i]];
				}
				stiffnessDistribution.ScaleForceVector(s, forces);
				lock (subdomainForces) subdomainForces[s] = forces;
			};
			environment.ExecuteSubdomainAction(model.Subdomains, subdomainAction);
			return subdomainForces;
		}

		/// <summary>
		/// WARNING: Only takes into account dof multiplicity for now.
		/// TODO: I should probably use Lpb matrices to match theory
		/// TODO: This should probably correct the values of boundary dofs so that they are the same across all subdomains 
		/// </summary>
		public virtual Vector GatherGlobalDisplacements(IStructuralModel model)
		{
			var globalDisplacements = Vector.CreateZero(model.GlobalDofOrdering.NumGlobalFreeDofs);
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				int id = subdomain.ID;
				int[] subdomainToGlobalDofs = model.GlobalDofOrdering.MapFreeDofsSubdomainToGlobal(subdomain);
				IVectorView uf = LinearSystems[id].Solution;
				for (int i = 0; i < subdomainToGlobalDofs.Length; ++i) //TODO: This should be provided by LinearAlgebra
				{
					globalDisplacements[subdomainToGlobalDofs[i]] = uf[i];
				}
			}
			return globalDisplacements;
		}

		public virtual void HandleMatrixWillBeSet()
		{
		}

		public virtual void Initialize()
		{
			dofSeparatorPsm.SeparateSubdomainDofsIntoBoundaryInternal();
			Action<ISubdomain> reorderInternalDofs = sub =>
			{
				matrixManagerPsm.ReorderInternalDofs(sub.ID);
			};
			environment.ExecuteSubdomainAction(model.Subdomains, reorderInternalDofs);

			dofSeparatorPsm.MapBoundaryDofsBetweenClusterSubdomains();
			stiffnessDistribution.CalcSubdomainScaling();

			Logger.LogNumDofs("Global boundary dofs", dofSeparatorPsm.GetNumBoundaryDofsCluster(clusterID));
		}

		public virtual Dictionary<int, Matrix> InverseSystemMatrixTimesOtherMatrix(Dictionary<int, IMatrixView> otherMatrix)
		{
			throw new NotImplementedException();
		}

		public virtual void OrderDofs(bool alsoOrderConstrainedDofs)
		{
			IGlobalFreeDofOrdering globalOrdering = dofOrderer.OrderFreeDofs(model);
			model.GlobalDofOrdering = globalOrdering;
			foreach (ISubdomain sub in model.Subdomains)
			{
				sub.FreeDofOrdering = globalOrdering.SubdomainDofOrderings[sub];
			}
		}

		public virtual void PreventFromOverwrittingSystemMatrices()
		{
		}

		public virtual void Solve()
		{
			Action<ISubdomain> calcSubdomainMatrices = sub =>
			{
				matrixManagerPsm.ExtractKiiKbbKib(sub.ID);
				matrixManagerPsm.InvertKii(sub.ID);
			};
			environment.ExecuteSubdomainAction(model.Subdomains, calcSubdomainMatrices);

			IInterfaceProblemMatrix interfaceProblemMatrix = 
				new InterfaceProblemMatrix(environment, model, dofSeparatorPsm, matrixManagerPsm);

			preconditioner.Calculate(interfaceProblemMatrix);
			SolveInterfaceProblem(interfaceProblemMatrix);
		}

		protected void SolveInterfaceProblem(IInterfaceProblemMatrix interfaceProblemMatrix)
		{
			rhsVectorManager.Clear();
			rhsVectorManager.CalcRhsVectors();

			bool initalGuessIsZero = !StartIterativeSolverFromPreviousSolution;
			if (!StartIterativeSolverFromPreviousSolution)
			{
				solutionVectorManager.Initialize();
			}
			IterativeStatistics stats = interfaceProblemSolver.Solve(interfaceProblemMatrix, preconditioner,
				rhsVectorManager.InterfaceProblemRhs, solutionVectorManager.GlobalBoundaryDisplacements, initalGuessIsZero);
			Logger.LogIterativeAlgorithm(stats.NumIterationsRequired, stats.ResidualNormRatioEstimation);
			Debug.WriteLine("Iterations for boundary problem = " + stats.NumIterationsRequired);

			solutionVectorManager.CalcSubdomainDisplacements();
			Logger.IncrementAnalysisStep();
		}

		public class Builder
		{
			public Builder() 
			{ 
				DofOrderer = new ReusingDofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());

				//TODO: perhaps use a custom convergence check like in FETI
				var pcgBuilder = new PcgAlgorithm.Builder();
				pcgBuilder.ResidualTolerance = 1E-6;
				pcgBuilder.MaxIterationsProvider = new PercentageMaxIterationsProvider(1.0);
				InterfaceProblemSolver = new InterfaceProblemSolverPcg(pcgBuilder.Build());
				IsHomogeneousProblem = true;

				MatrixManagerFactory = new PsmMatrixManagerSymmetricCSparse.Factory();
				Preconditioner = new PsmPreconditionerIdentity();
			}

			public IDofOrderer DofOrderer { get; set; }

			public IDdmEnvironment ComputingEnvironment { get; set; } = new ProcessingEnvironment(
				new SubdomainEnvironmentManagedSequential(), new ClusterEnvironmentManagedSequential());

			public IInterfaceProblemSolver InterfaceProblemSolver { get; set; }

			public bool IsHomogeneousProblem { get; set; }

			public IPsmMatrixManagerFactory MatrixManagerFactory { get; set; }

			public IPsmPreconditioner Preconditioner { get; set; }

			public PsmSolver BuildSolver(IStructuralModel model, IList<Cluster> clusters)
			{
				var dofSeparator = new PsmDofSeparator(ComputingEnvironment, model, clusters);
				var (matrixManagerBasic, matrixManagerPsm) = MatrixManagerFactory.CreateMatrixManagers(model, dofSeparator);
				return new PsmSolver(ComputingEnvironment, model, clusters, DofOrderer, dofSeparator, matrixManagerBasic,
					matrixManagerPsm, Preconditioner, InterfaceProblemSolver, IsHomogeneousProblem);
			}
		}
	}
}
