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
using System.Linq;
using MGroup.Solvers.Distributed.Topologies;
using MGroup.Solvers.Distributed.LinearAlgebra;
using MGroup.Solvers.DDM.Dofs;

namespace MGroup.Solvers.DDM.Psm
{
	public class PsmSolver_NEW : ISolver
	{
		protected const int clusterID = 0;

		protected readonly IDofOrderer dofOrderer;
		protected readonly IPsmDofSeparator_NEW dofSeparatorPsm;
		protected readonly IDdmEnvironment environment;
		protected readonly IInterfaceProblemSolver interfaceProblemSolver;
		protected readonly IMatrixManager matrixManagerBasic;
		protected readonly IPsmMatrixManager matrixManagerPsm;
		protected readonly IStructuralModel model;
		private readonly ClusterTopology clusterTopology;
		protected readonly string name;
		protected readonly IPsmPreconditioner preconditioner;
		protected readonly IPsmRhsVectorManager rhsVectorManager;
		protected readonly IPsmSolutionVectorManager solutionVectorManager;
		protected readonly IStiffnessDistribution stiffnessDistribution;

		private DistributedIndexer indexer; //TODOMPI: make this private and a single objects, instead of a Dictionary.

		protected PsmSolver_NEW(IDdmEnvironment environment, IStructuralModel model, ClusterTopology clusterTopology,
			IDofOrderer dofOrderer, IPsmDofSeparator_NEW dofSeparator, IMatrixManager matrixManagerBasic,
			IPsmMatrixManager matrixManagerPsm, IPsmPreconditioner preconditioner, IInterfaceProblemSolver interfaceProblemSolver,
			bool isHomogeneous, string name = "PSM Solver")
		{
			this.name = name;
			this.environment = environment;

			this.model = model;
			this.clusterTopology = clusterTopology;
			this.dofOrderer = dofOrderer;
			this.dofSeparatorPsm = dofSeparator;
			this.matrixManagerBasic = matrixManagerBasic;
			this.matrixManagerPsm = matrixManagerPsm;
			this.preconditioner = preconditioner;
			this.interfaceProblemSolver = interfaceProblemSolver;

			Cluster[] clusters = clusterTopology.Clusters.Values.ToArray();
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

			// Initialize(); //TODOMPI: This used to run here, but now the order of operations must be revised.

			return matricesKff;
		}

		public virtual Dictionary<int, (IMatrix matrixFreeFree, IMatrixView matrixFreeConstr, IMatrixView matrixConstrFree, 
			IMatrixView matrixConstrConstr)> BuildGlobalSubmatrices(IElementMatrixProvider elementMatrixProvider)
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
		public virtual Vector GatherGlobalDisplacements(IStructuralModel model) //TODOMPI: global-level vectors must be avoided. Perhaps in extension methods.
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

		public void InitializeClusterTopology()
		{
			//TODOMPI: To perform these, I must first have some basic knowledge of existing clusters and subdomains / cluster.
			//		Only the neighborhoods can be done later.
			clusterTopology.FindClustersOfSubdomains();
			clusterTopology.FindNeighboringClusters();
			environment.ClusterTopology = clusterTopology;

			foreach (Cluster cluster in clusterTopology.Clusters.Values)
			{
				ComputeNode thisNode = environment.ComputeEnvironment.NodeTopology.Nodes[cluster.ID];
				foreach (int neighbor in cluster.InterClusterNodes.Keys)
				{
					ComputeNode otherNode = environment.ComputeEnvironment.NodeTopology.Nodes[neighbor];
					thisNode.Neighbors.Add(otherNode);
				}
			}
		}

		public virtual void Initialize()
		{
			dofSeparatorPsm.SeparateSubdomainDofsIntoBoundaryInternal();
			Action<ISubdomain> reorderInternalDofs = sub =>
			{
				matrixManagerPsm.ReorderInternalDofs(sub.ID);
			};
			environment.ExecuteSubdomainAction(model.Subdomains, reorderInternalDofs);

			dofSeparatorPsm.OrderBoundaryDofsOfClusters();
			dofSeparatorPsm.MapBoundaryDofsBetweenClusterSubdomains();
			stiffnessDistribution.CalcSubdomainScaling();

			CreateDistributedIndexer();

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
			//Action<ISubdomain> calcSubdomainMatrices = sub =>
			//{
			//	matrixManagerPsm.ExtractKiiKbbKib(sub.ID);
			//	matrixManagerPsm.InvertKii(sub.ID);
			//};
			//environment.ExecuteSubdomainAction(model.Subdomains, calcSubdomainMatrices);

			//IInterfaceProblemMatrix interfaceProblemMatrix = 
			//	new InterfaceProblemMatrix(environment, model, dofSeparatorPsm, matrixManagerPsm);

			//preconditioner.Calculate(interfaceProblemMatrix);
			//SolveInterfaceProblem(interfaceProblemMatrix);
		}

		protected void SolveInterfaceProblem(IInterfaceProblemMatrix interfaceProblemMatrix)
		{
			//rhsVectorManager.Clear();
			//rhsVectorManager.CalcRhsVectors();

			//bool initalGuessIsZero = !StartIterativeSolverFromPreviousSolution;
			//if (!StartIterativeSolverFromPreviousSolution)
			//{
			//	solutionVectorManager.Initialize();
			//}
			//IterativeStatistics stats = interfaceProblemSolver.Solve(interfaceProblemMatrix, preconditioner,
			//	rhsVectorManager.InterfaceProblemRhs, solutionVectorManager.GlobalBoundaryDisplacements, initalGuessIsZero);
			//Logger.LogIterativeAlgorithm(stats.NumIterationsRequired, stats.ResidualNormRatioEstimation);
			//Debug.WriteLine("Iterations for boundary problem = " + stats.NumIterationsRequired);

			//solutionVectorManager.CalcSubdomainDisplacements();
			//Logger.IncrementAnalysisStep();
		}

		private void CreateDistributedIndexer()
		{
			indexer = new DistributedIndexer(environment.ComputeEnvironment.NodeTopology.Nodes.Values);
			foreach (ComputeNode computeNode in environment.ComputeEnvironment.NodeTopology.Nodes.Values) //TODOMPI: parallelize this
			{
				Cluster cluster = environment.GetClusterOfComputeNode(computeNode);
				int numBoundaryDofsOfCluster = dofSeparatorPsm.GetNumBoundaryDofsCluster(cluster.ID);
				DofTable boundaryDofsOfCluster = dofSeparatorPsm.GetClusterDofOrderingBoundary(cluster.ID);

				var interClusterDofs = new Dictionary<ComputeNode, int[]>();
				foreach (ComputeNode neighbor in computeNode.Neighbors)
				{
					int neighborClusterID = neighbor.ID;
					SortedSet<int> commonNodes = cluster.InterClusterNodes[neighborClusterID];
					var commonDofs = new List<int>(2 * commonNodes.Count);
					foreach (int nodeID in commonNodes)
					{
						INode node = model.GetNode(nodeID);
						IReadOnlyDictionary<IDofType, int> dofIndices = boundaryDofsOfCluster.GetDataOfRow(node);

						// The dofs of each node must be in the same order across all clusters. 
						// Use the ids of the IDofType to sort them.
						var orderedDofIndices = dofIndices.OrderBy(pair => AllDofs.GetIdOfDof(pair.Key));
						foreach (var dofIdxPair in orderedDofIndices)
						{
							commonDofs.Add(dofIdxPair.Value);
						}
					}
					interClusterDofs[neighbor] = commonDofs.ToArray();
				}

				indexer.ConfigureForNode(computeNode, numBoundaryDofsOfCluster, interClusterDofs);
			}
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

				MatrixManagerFactory = new PsmMatrixManagerCSparse.Factory();
				Preconditioner = new PsmPreconditionerIdentity();
			}

			public IDofOrderer DofOrderer { get; set; }

			public IDdmEnvironment ComputingEnvironment { get; set; } = new ProcessingEnvironment(
				new SubdomainEnvironmentManagedSequential(), new ClusterEnvironmentManagedSequential());

			public IInterfaceProblemSolver InterfaceProblemSolver { get; set; }

			public bool IsHomogeneousProblem { get; set; }

			public IPsmMatrixManagerFactory MatrixManagerFactory { get; set; }

			public IPsmPreconditioner Preconditioner { get; set; }

			public PsmSolver_NEW BuildSolver(IStructuralModel model, ClusterTopology clusterTopology)
			{
				var dofSeparator = new PsmDofSeparator_NEW(ComputingEnvironment, model, clusterTopology);
				var (matrixManagerBasic, matrixManagerPsm) = MatrixManagerFactory.CreateMatrixManagers(model, dofSeparator);
				return new PsmSolver_NEW(ComputingEnvironment, model, clusterTopology, DofOrderer, dofSeparator, 
					matrixManagerBasic, matrixManagerPsm, Preconditioner, InterfaceProblemSolver, IsHomogeneousProblem);
			}
		}
	}
}
