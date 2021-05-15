using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.Commons;
using MGroup.Solvers.DDM.Dofs;
using MGroup.Solvers.DDM.Environments;
using MGroup.Solvers.DDM.Mappings;
using MGroup.Solvers.Distributed.Environments;
using MGroup.Solvers.Distributed.Topologies;
using MGroup.Solvers.DofOrdering.Reordering;

//TODOMPI: Replace DofTable with an equivalent class that uses integers. Also allow clients to choose sorted versions
//TODOMPI: Remove all locking mechanics from DDM classes. These should be employed by whatever environments need them. Instead
//		the environments should expose methods such as CreateNodeDictionary<ComputeNode, T>(), 
//		CreateSubNodeDictionary<ComputeSubNode, T>() to accumulate data by initializing dictionaries or similar ones to 
//		accumulate data by populating existing dictionaries. Also it is very important to be able to populate multiple 
//		dictionaries in the same call, for example: 
//		Dictionary<int, Matrix> Kbb, Dictionary<int, CsrMatrix> Kbi, Dictionary<int, SuiteSparseCholeskyFactorization> invKii
namespace MGroup.Solvers.DDM.Psm.Dofs
{
	public class PsmDofSeparator_NEW : IPsmDofSeparator_NEW
	{
		private readonly IComputeEnvironment environment;
		private readonly IStructuralModel model;
		private readonly ClusterTopology clusterTopology;

		private readonly Dictionary<int, DofTable> clusterDofOrderingsBoundary = new Dictionary<int, DofTable>();
		private readonly Dictionary<int, int> clusterNumBoundaryDofs = new Dictionary<int, int>();
		private readonly Dictionary<int, DofTable> subdomainDofOrderingsBoundary = new Dictionary<int, DofTable>();
		private readonly Dictionary<int, int[]> subdomainDofsBoundaryToFree = new Dictionary<int, int[]>();
		private readonly Dictionary<int, int[]> subdomainDofsInternalToFree = new Dictionary<int, int[]>();
		private readonly Dictionary<int, BooleanMatrixRowsToColumns> subdomainToClusterBoundaryMappings = 
			new Dictionary<int, BooleanMatrixRowsToColumns>();
		private readonly Dictionary<int, int> subdomainNumFreeDofs = new Dictionary<int, int>();

		public PsmDofSeparator_NEW(IComputeEnvironment environment, IStructuralModel model, ClusterTopology clusterTopology)
		{
			this.environment = environment;
			this.model = model;
			this.clusterTopology = clusterTopology;
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
			Action<ComputeSubnode> subdomainAction = computeSubnode =>
			{
				ISubdomain subdomain = model.GetSubdomain(computeSubnode.ID);

				DofTable boundaryDofOrderingOfCluster = environment.AccessNodeDataFromSubnode(computeSubnode,
					computeNode => clusterDofOrderingsBoundary[computeNode.ID]);

				BooleanMatrixRowsToColumns Lb =
					MapDofsClusterToSubdomain(boundaryDofOrderingOfCluster, subdomainDofOrderingsBoundary[subdomain.ID]);
				lock (subdomainToClusterBoundaryMappings) subdomainToClusterBoundaryMappings[subdomain.ID] = Lb;
			};
			environment.DoPerSubnode(subdomainAction);
		}

		public void OrderBoundaryDofsOfClusters() //TODO: Replace comments with more descriptive method names.
		{
			// Find boundary dofs defined by subdomains of this cluster
			Dictionary<ComputeNode, DofSet> boundaryDofsPerCluster = FindBoundaryDofsPerCluster();

			// Merge these with boundary dofs defined by subdomains of other clusters, since they may be different
			ExchangeInterClusterDofsWithNeighbors(boundaryDofsPerCluster);

			// Each cluster orders all boundary dofs, including the ones used by subdomains that do not belong to it
			OrderBoundaryDofsPerCluster(boundaryDofsPerCluster);
		}

		public void ReorderSubdomainInternalDofs(int subdomainID, DofPermutation permutation)
		{
			if (permutation.IsBetter)
			{
				// This code may run concurrently by multiple threads. There is no need for a lock, since the Dictionary is not 
				// changed, only the values of existing keys may be updated.
				int[] internalDofs = permutation.ReorderKeysOfDofIndicesMap(subdomainDofsInternalToFree[subdomainID]);
				subdomainDofsInternalToFree[subdomainID] = internalDofs; 
			}
		}

		/// <summary>
		/// Boundary/internal dofs
		/// </summary>
		public void SeparateSubdomainDofsIntoBoundaryInternal()
		{
			Action<ComputeSubnode> subdomainAction = computeSubnode =>
			{
				ISubdomain subdomain = model.GetSubdomain(computeSubnode.ID);
				int s = subdomain.ID;
				(DofTable boundaryDofOrdering, int[] boundaryToFree, int[] internalToFree) = SeparateSubdomainDofs(subdomain);

				//TODO: Perhaps I should have one lock for all of these. There is marginal gain if any by all these locks.
				//		On the other hand this would be safer, if I modified these dictionaries elsewhere, which I do not. 
				lock (subdomainNumFreeDofs) subdomainNumFreeDofs[s] = subdomain.FreeDofOrdering.NumFreeDofs;
				lock (subdomainDofOrderingsBoundary) subdomainDofOrderingsBoundary[s] = boundaryDofOrdering;
				lock (subdomainDofsBoundaryToFree) subdomainDofsBoundaryToFree[s] = boundaryToFree;
				lock (subdomainDofsInternalToFree) subdomainDofsInternalToFree[s] = internalToFree;
			};
			environment.DoPerSubnode(subdomainAction);
		}

		private void ExchangeInterClusterDofsWithNeighbors(Dictionary<ComputeNode, DofSet> boundaryDofsPerCluster)
		{
			if (environment.NumComputeNodes > 1) return;

			// Each cluster collects the dofs it will send to each of its neighbors
			Func<ComputeNode, AllToAllNodeData<int>> prepareTransferData = computeNode =>
			{
				Cluster cluster = clusterTopology.Clusters[computeNode.ID];
				int numNeighbors = computeNode.Neighbors.Count;

				var transferData = new AllToAllNodeData<int>(numNeighbors);
				for (int n = 0; n < numNeighbors; ++n)
				{
					int neighborID = computeNode.Neighbors[n].ID;
					SortedSet<int> commonNodes = cluster.InterClusterNodes[neighborID];
					transferData.sendValues[n] = boundaryDofsPerCluster[computeNode].PackDofsOfNodes(commonNodes);
				}

				// No buffers for receive values yet, since their lengths are unknown. 
				// Let the environment create them by extra communication.
				return transferData;
			};
			Dictionary<ComputeNode, AllToAllNodeData<int>> transferDataPerCluster = 
				environment.CreateDictionaryPerNode(prepareTransferData);

			// Perform the communications
			environment.NeighborhoodAllToAllForNodes(transferDataPerCluster, false);

			// Each cluster integrates the dofs it received with its own ones
			Action<ComputeNode> processReceivedDofs = computeNode =>
			{
				Cluster cluster = clusterTopology.Clusters[computeNode.ID];
				int numNeighbors = computeNode.Neighbors.Count;

				for (int n = 0; n < numNeighbors; ++n)
				{
					int neighborID = computeNode.Neighbors[n].ID;
					int[] receivedDofs = transferDataPerCluster[computeNode].recvValues[n];
					boundaryDofsPerCluster[computeNode].UnpackDofsAndUnion(receivedDofs);
				}
			};
			environment.DoPerNode(processReceivedDofs);
		}


		//TODOMPI: Consider a dedicated class for SortedDictionary<int, SortedSet<int>>. It will help collecting the dofs, 
		//	communicating them, incoroparating dofs from other clusters and finally number them.
		private Dictionary<ComputeNode, DofSet> FindBoundaryDofsPerCluster()
		{
			Func<ComputeNode, DofSet> findBoundaryDofsPerCluster = computeNode =>
			{
				Cluster cluster = clusterTopology.Clusters[computeNode.ID];
				var dofsOfBoundaryNodes = new DofSet();
				
				//TODOMPI: Should this be parallelized? It is similar to assembling a cluster-level vector from subdomain-level 
				//		subvectors. I do not think the effort is worth it. Nevertheless, this code assumes a sequential order of 
				//		operations. Shouldn't the environment decide the order, even if that means havinf a method, 
				//		DoSequentially(Action), or even better AssembleClusterDataStructureFromSubdomains(...)?
				foreach (ComputeSubnode computeSubnode in computeNode.Subnodes.Values)
				{
					ISubdomain subdomain = model.GetSubdomain(computeSubnode.ID);

					//TODOMPI: Alternatively, I can just take all nodes of the cluster and see if they are boundary. However, how would I
					//		find their dofs? Do I need a DofTable for free dofs of each cluster? So far only boundary dofs of a cluster 
					//		are put in a cluster-level vector.
					DofTable subdomainFreeDofs = environment.AccessSubnodeDataFromNode(computeSubnode,
						subnode => model.GetSubdomain(subnode.ID).FreeDofOrdering.FreeDofs);

					//TODOMPI: Perhaps I should only iterate over boundary dofs of each subdomain. Less entries to go through, 
					//		with added benefits if communication is needed
					foreach (INode node in subdomain.Nodes)
					{
						if (node.GetMultiplicity() == 1) continue; // internal node
						dofsOfBoundaryNodes.AddDofs(node, subdomainFreeDofs.GetColumnsOfRow(node));
					}
				}
				return dofsOfBoundaryNodes;
			};
			return environment.CreateDictionaryPerNode(findBoundaryDofsPerCluster);
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

		private void OrderBoundaryDofsPerCluster(Dictionary<ComputeNode, DofSet> boundaryDofsPerCluster)
		{
			Action<ComputeNode> orderBoundaryDofsPerCluster = computeNode =>
			{
				// Order all boundary dofs for this cluster
				Cluster cluster = clusterTopology.Clusters[computeNode.ID];
				DofSet dofsOfBoundaryNodes = boundaryDofsPerCluster[computeNode];
				(int numBoundaryDofs, DofTable boundaryDofOrdering) = dofsOfBoundaryNodes.OrderDofs(n => model.GetNode(n));

				// Store them
				lock (clusterDofOrderingsBoundary) clusterDofOrderingsBoundary[cluster.ID] = boundaryDofOrdering;
				lock (clusterNumBoundaryDofs) clusterNumBoundaryDofs[cluster.ID] = numBoundaryDofs;

			};
			environment.DoPerNode(orderBoundaryDofsPerCluster);
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
