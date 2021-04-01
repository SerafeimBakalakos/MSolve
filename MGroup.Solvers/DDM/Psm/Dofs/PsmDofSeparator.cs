using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.DDM.Environments;
using MGroup.Solvers.DDM.Mappings;
using MGroup.Solvers.DofOrdering.Reordering;

namespace MGroup.Solvers.DDM.Psm.Dofs
{
	public class PsmDofSeparator : IPsmDofSeparator
	{
		private readonly IProcessingEnvironment environment;
		private readonly IStructuralModel model;
		private readonly IList<Cluster> clusters; //TODOMPI: Should this be a list? Each process has only 1 cluster. The only usecase is if we use >1 clusters in serial code for easier debuging.

		private readonly Dictionary<int, DofTable> clusterDofOrderingsBoundary = new Dictionary<int, DofTable>();
		private readonly Dictionary<int, int> clusterNumBoundaryDofs = new Dictionary<int, int>();
		private readonly Dictionary<int, DofTable> subdomainDofOrderingsBoundary = new Dictionary<int, DofTable>();
		private readonly Dictionary<int, int[]> subdomainDofsBoundaryToFree = new Dictionary<int, int[]>();
		private readonly Dictionary<int, int[]> subdomainDofsInternalToFree = new Dictionary<int, int[]>();
		private readonly Dictionary<int, BooleanMatrixRowsToColumns> subdomainToClusterBoundaryMappings = 
			new Dictionary<int, BooleanMatrixRowsToColumns>();
		private readonly Dictionary<int, int> subdomainNumFreeDofs = new Dictionary<int, int>();

		public PsmDofSeparator(IProcessingEnvironment environment, IStructuralModel model, IList<Cluster> clusters)
		{
			this.environment = environment;
			this.model = model;
			this.clusters = clusters;
		}

		public DofTable GetClusterDofOrderingBoundary(int clusterID) => clusterDofOrderingsBoundary[clusterID];

		public int GetNumBoundaryDofsCluster(int clusterID) => clusterNumBoundaryDofs[clusterID];

		public DofTable GetSubdomainDofOrderingBoundary(int subdomainID) => subdomainDofOrderingsBoundary[subdomainID];

		public int[] GetSubdomainDofsBoundaryToFree(int subdomainID) => subdomainDofsBoundaryToFree[subdomainID];

		public int[] GetSubdomainDofsInternalToFree(int subdomainID) => subdomainDofsInternalToFree[subdomainID];

		public int GetNumFreeDofsSubdomain(int subdomainID) => subdomainNumFreeDofs[subdomainID];

		public BooleanMatrixRowsToColumns GetDofMappingBoundaryClusterToSubdomain(int subdomainID) 
			=> subdomainToClusterBoundaryMappings[subdomainID];

		/// <summary>
		/// Lb mappings: subdomain to/from cluster
		/// </summary>
		public void MapBoundaryDofsBetweenClusterSubdomains()
		{
			Action<Cluster> clusterAction = cluster =>
			{
				(int numBoundaryDofs, DofTable boundaryDofOrdering) = OrderClusterBoundaryDofs(cluster);
				lock (clusterDofOrderingsBoundary) clusterDofOrderingsBoundary[cluster.ID] = boundaryDofOrdering;
				lock (clusterNumBoundaryDofs) clusterNumBoundaryDofs[cluster.ID] = numBoundaryDofs;

				Action<ISubdomain> subdomainAction = subdomain =>
				{
					BooleanMatrixRowsToColumns Lb = 
						MapDofsClusterToSubdomain(boundaryDofOrdering, subdomainDofOrderingsBoundary[subdomain.ID]);
					lock (subdomainToClusterBoundaryMappings) subdomainToClusterBoundaryMappings[subdomain.ID] = Lb;
				};
				environment.ExecuteSubdomainAction(cluster.Subdomains, subdomainAction);
			};
			environment.ExecuteClusterAction(clusters, clusterAction);
		}

		public void ReorderSubdomainInternalDofs(int subdomainID, DofPermutation permutation)
		{
			if (permutation.IsBetter)
			{
				int[] internalDofs = permutation.ReorderKeysOfDofIndicesMap(subdomainDofsInternalToFree[subdomainID]);
				lock (subdomainDofsInternalToFree) subdomainDofsInternalToFree[subdomainID] = internalDofs;
			}
		}

		/// <summary>
		/// Boundary/internal dofs
		/// </summary>
		public void SeparateSubdomainDofsIntoBoundaryInternal()
		{
			Action<Cluster> clusterAction = cluster =>
			{
				// Boundary - Internal dofs
				Action<ISubdomain> subdomainAction = subdomain =>
				{
					int s = subdomain.ID;
					(DofTable boundaryDofOrdering, int[] boundaryToFree, int[] internalToFree) = SeparateSubdomainDofs(subdomain);
					lock (subdomainNumFreeDofs) subdomainNumFreeDofs[s] = subdomain.FreeDofOrdering.NumFreeDofs;
					lock (subdomainDofOrderingsBoundary) subdomainDofOrderingsBoundary[s] = boundaryDofOrdering;
					lock (subdomainDofsBoundaryToFree) subdomainDofsBoundaryToFree[s] = boundaryToFree;
					lock (subdomainDofsInternalToFree) subdomainDofsInternalToFree[s] = internalToFree;
				};
				environment.ExecuteSubdomainAction(cluster.Subdomains, subdomainAction);
			};
			environment.ExecuteClusterAction(clusters, clusterAction);
		}

		private static BooleanMatrixRowsToColumns MapDofsClusterToSubdomain(
			DofTable clusterBoundaryDofOrdering, DofTable subdomainBoundaryDofOrdering)
		{
			int numClusterBoundaryDofs = clusterBoundaryDofOrdering.EntryCount;
			int numSubdomainBoundaryDofs = subdomainBoundaryDofOrdering.EntryCount;
			var nonZeroColOfRow = new int[numSubdomainBoundaryDofs];
			foreach ((INode node, IDofType dof, int subdomainIdx) in subdomainBoundaryDofOrdering)
			{
				int clusterIdx = clusterBoundaryDofOrdering[node, dof];
				nonZeroColOfRow[subdomainIdx] = clusterIdx;
			}
			return new BooleanMatrixRowsToColumns(numSubdomainBoundaryDofs, numClusterBoundaryDofs, nonZeroColOfRow);
		}

		private static (int numBoundaryDofs, DofTable boundaryDofOrdering) OrderClusterBoundaryDofs(Cluster cluster)
		{
			var boundaryDofOrdering = new DofTable();
			int numBoundaryDofs = 0;
			foreach (ISubdomain subdomain in cluster.Subdomains) // Not worth the parallelization difficulty
			{
				foreach ((INode node, IDofType dof, int idx) in subdomain.FreeDofOrdering.FreeDofs)
				{
					if (node.GetMultiplicity() > 1)
					{
						bool didNotExist = boundaryDofOrdering.TryAdd(node, dof, numBoundaryDofs);
						if (didNotExist)
						{
							numBoundaryDofs++;
						}
					}
				}
			}
			return (numBoundaryDofs, boundaryDofOrdering);
		}

		private static (DofTable boundaryDofOrdering, int[] boundaryToFree, int[] internalToFree) SeparateSubdomainDofs(
			ISubdomain subdomain)
		{
			var boundaryDofOrdering = new DofTable();
			var boundaryToFree = new List<int>();
			var internalToFree = new HashSet<int>();
			int subdomainBoundaryIdx = 0;
			DofTable freeDofs = subdomain.FreeDofOrdering.FreeDofs;
			IEnumerable<INode> nodes = freeDofs.GetRows();
			foreach (INode node in nodes) //TODO: Optimize access: Directly get INode, Dictionary<IDof, int>
			{
				IReadOnlyDictionary<IDofType, int> dofsOfNode = freeDofs.GetDataOfRow(node);
				if (node.GetMultiplicity() > 1)
				{
					foreach (var dofTypeIdxPair in dofsOfNode)
					{
						boundaryDofOrdering[node, dofTypeIdxPair.Key] = subdomainBoundaryIdx++;
						boundaryToFree.Add(dofTypeIdxPair.Value);
					}
				}
				else
				{
					foreach (var dofTypeIdxPair in dofsOfNode)
					{
						internalToFree.Add(dofTypeIdxPair.Value);
					}
				}
			}

			return (boundaryDofOrdering, boundaryToFree.ToArray(), internalToFree.ToArray());
		}
	}
}
