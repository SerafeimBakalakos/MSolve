using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers_OLD.DDM.Environments;
using MGroup.Solvers_OLD.DDM.Mappings;
using MGroup.Solvers_OLD.DDM.PFetiDP.Dofs;
using MGroup.Solvers_OLD.DDM.Psm.Dofs;
using MGroup.Solvers_OLD.DDM.StiffnessMatrices;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using System.Collections.Concurrent;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.Environments;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.Topologies;
using MGroup.Solvers_OLD.DDM;

namespace MGroup.Solvers_OLD.DistributedTry1.DDM.Psm.StiffnessDistribution
{
	public class HeterogeneousStiffnessDistribution_NEW : IStiffnessDistribution_NEW
	{
		private readonly IComputeEnvironment environment;
		private readonly IStructuralModel model;
		private readonly ClusterTopology clusterTopology;
		private readonly IPsmDofSeparator dofSeparator;
		private readonly IMatrixManager matrixManagerBasic;
		private readonly ConcurrentDictionary<int, IMappingMatrix> dofMappingBoundaryClusterToSubdomain = 
			new ConcurrentDictionary<int, IMappingMatrix>();
		private readonly ConcurrentDictionary<int, double[]> relativeStiffnesses = new ConcurrentDictionary<int, double[]>();

		public HeterogeneousStiffnessDistribution_NEW(IComputeEnvironment environment, IStructuralModel model, 
			ClusterTopology clusterTopology, IPsmDofSeparator dofSeparator, IMatrixManager matrixManagerBasic)
		{
			this.environment = environment;
			this.model = model;
			this.clusterTopology = clusterTopology;
			this.dofSeparator = dofSeparator;
			this.matrixManagerBasic = matrixManagerBasic;
		}

		/// <summary>
		/// See eq (6.3) from Papagiannakis bachelor :
		/// Lpb^e = Db^e * Lb^e * inv( (Lb^e)^T * Db^e * Lb^e)
		/// </summary>
		public void CalcSubdomainScaling()
		{
			// Build Db^s from each subdomain's Kff
			Func<ComputeSubnode, double[]> calcSubdomainDb = computeSubnode =>
			{
				ISubdomain subdomain = model.GetSubdomain(computeSubnode.ID);
				int[] boundaryDofs = dofSeparator.GetSubdomainDofsBoundaryToFree(subdomain.ID);
				IMatrixView Kff = matrixManagerBasic.GetLinearSystem(subdomain.ID).Matrix;

				//TODO: This an operation that should be provided by the LinearAlgebra project. Interface IDiagonalizable 
				//		with methods: GetDiagonal() and GetSubdiagonal(int[] indices). Optimized versions for most storage
				//		formats are possible. E.g. for Symmetric CSR/CSC with ordered indices, the diagonal entry is the 
				//		last of each row/col. For general CSC/CSC with ordered indices, bisection can be used for to locate
				//		the diagonal entry of each row/col in log(nnzPerRow). In any case these should be hidden from DDM classes.
				var Db = new double[boundaryDofs.Length];
				for (int boundaryDofIdx = 0; boundaryDofIdx < boundaryDofs.Length; boundaryDofIdx++)
				{
					int freeDofIdx = boundaryDofs[boundaryDofIdx];
					Db[boundaryDofIdx] = Kff[freeDofIdx, freeDofIdx];
				}
				return Db;
			};
			Dictionary<int, double[]> subdomainMatricesDb = environment.CreateDictionaryPerSubnode(calcSubdomainDb);

			Func<ComputeNode, double[]> clusterAction = computeNode =>
			{
				Cluster cluster = clusterTopology.Clusters[computeNode.ID];

				// Assemble subdomain Db^s matrices into cluster's (Lb^e)^T * Db^e * Lb^e)
				var clusterDb = new double[dofSeparator.GetNumBoundaryDofsCluster(cluster.ID)];
				foreach (ComputeSubnode computeSubnode in computeNode.Subnodes.Values)
				{
					ISubdomain subdomain = model.GetSubdomain(computeSubnode.ID);
					double[] subdomainDb = environment.AccessSubnodeDataFromNode(computeSubnode, 
						subnode => subdomainMatricesDb[subnode.ID]);
					int[] subdomainToCluster = dofSeparator.GetDofMappingBoundaryClusterToSubdomain(subdomain.ID).RowsToColumns; 
					for (int i = 0; i < subdomainToCluster.Length; i++)
					{
						clusterDb[subdomainToCluster[i]] += subdomainDb[i];
					}
				}
				return clusterDb;
			};
			Dictionary<ComputeNode, double[]> clusterMatricesDb = environment.CreateDictionaryPerNode(clusterAction);
			throw new NotImplementedException("We must exchange Db between clusters at this point");

			// Calculate Lpb^s = Db^s * Lb^s * inv( (Lb^e)^T * Db^e * Lb^e) )
			Action<ComputeSubnode> calcSubdomainLpb = computeSubnode =>
			{
				ISubdomain subdomain = model.GetSubdomain(computeSubnode.ID);

				double[] clusterDb = environment.AccessNodeDataFromSubnode(computeSubnode,
					computeNode => clusterMatricesDb[computeNode]);
				double[] subdomainDb = subdomainMatricesDb[subdomain.ID];
				BooleanMatrixRowsToColumns Lb = dofSeparator.GetDofMappingBoundaryClusterToSubdomain(subdomain.ID);

				//TODO: perhaps Lb should not be a dedicated matrix class, but directly and int[] array. These RowsToColumns are 
				//		confusing and feel like a private detail of that class. Especially if multiplications Lb * vector are 
				//		hidden in DistribuitedMatrix classes, the advantage of following the theory close will be removed.
				int[] subdomainToCluster = Lb.RowsToColumns; 
				var subdomainRelativeStiffness = new double[subdomainDb.Length];
				for (int i = 0; i < subdomainDb.Length; i++)
				{
					subdomainRelativeStiffness[i] = subdomainDb[i] / clusterDb[subdomainToCluster[i]];
				}

				var Lpb = new ScalingMatrixRowMajor(
					Lb.NumRows, Lb.NumColumns, Lb.RowsToColumns, relativeStiffnesses[subdomain.ID]);

				relativeStiffnesses[subdomain.ID] = subdomainRelativeStiffness;
				dofMappingBoundaryClusterToSubdomain[subdomain.ID] = Lpb;
			};
			environment.DoPerSubnode(calcSubdomainLpb);
		}
		

		public IMappingMatrix GetDofMappingBoundaryClusterToSubdomain(int subdomainID) 
			=> dofMappingBoundaryClusterToSubdomain[subdomainID];

		public Dictionary<int, SparseVector> DistributeNodalLoads(Table<INode, IDofType, double> nodalLoads)
		{
			Func<ComputeSubnode, SparseVector> calcSubdomainForces = computeSubnode =>
			{
				ISubdomain subdomain = model.GetSubdomain(computeSubnode.ID);
				DofTable freeDofs = subdomain.FreeDofOrdering.FreeDofs;
				DofTable boundaryDofs = dofSeparator.GetSubdomainDofOrderingBoundary(subdomain.ID);
				double[] coefficients = relativeStiffnesses[subdomain.ID];

				//TODO: I go through every node and ignore the ones that are not loaded. 
				//		It would be better to directly access the loaded ones.
				var nonZeroLoads = new SortedDictionary<int, double>();
				foreach (INode node in subdomain.Nodes)
				{
					bool isLoaded = nodalLoads.TryGetDataOfRow(node, out IReadOnlyDictionary<IDofType, double> loadsOfNode);
					if (!isLoaded) continue;

					if (node.GetMultiplicity() == 1) // optimization for internal dofs
					{
						foreach (var dofLoadPair in loadsOfNode)
						{
							int freeDofIdx = freeDofs[node, dofLoadPair.Key];
							nonZeroLoads[freeDofIdx] = dofLoadPair.Value / node.GetMultiplicity();
						}
					}
					else
					{
						foreach (var dofLoadPair in loadsOfNode)
						{
							int freeDofIdx = freeDofs[node, dofLoadPair.Key];
							int boundaryDofIdx = boundaryDofs[node, dofLoadPair.Key];
							nonZeroLoads[freeDofIdx] = dofLoadPair.Value * coefficients[boundaryDofIdx];
						}
					}
				}

				return SparseVector.CreateFromDictionary(subdomain.FreeDofOrdering.NumFreeDofs, nonZeroLoads);
			};
			return environment.CreateDictionaryPerSubnode(calcSubdomainForces);
		}

		public void ScaleForceVector(int subdomainID, Vector subdomainForces)
		{
			int[] boundaryDofs = dofSeparator.GetSubdomainDofsBoundaryToFree(subdomainID);
			double[] relativeStiffnessOfSubdomain = relativeStiffnesses[subdomainID];
			for (int i = 0; i < boundaryDofs.Length; i++)
			{
				double coeff = relativeStiffnessOfSubdomain[i];
				subdomainForces[boundaryDofs[i]] *= coeff;
			}
		}
	}
}
