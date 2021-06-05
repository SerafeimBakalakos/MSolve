using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Environments;
using MGroup.LinearAlgebra.Distributed.Overlapping;
using MGroup.Solvers.Commons;
using MGroup.Solvers.DomainDecomposition.Commons;

//TODOMPI: Replace DofTable with an equivalent class that uses integers. Also allow clients to choose sorted versions
//TODOMPI: Instead of going to asking ComputeNode for its neighbors, ISubdomain should contain this info. 
namespace MGroup.Solvers.DomainDecomposition.PSM.Dofs
{
	public class PsmDofSeparator : IPsmDofSeparator
	{
		private readonly IComputeEnvironment environment;
		private readonly IStructuralModel model;

		//TODO: This is essential for testing and very useful for debugging, but not production code. Should I remove it?
		private readonly bool sortDofsWhenPossible;

		private readonly SubdomainTopology subdomainTopology;
		private readonly ConcurrentDictionary<int, DofTable> subdomainDofOrderingsBoundary = 
			new ConcurrentDictionary<int, DofTable>();
		private readonly ConcurrentDictionary<int, int[]> subdomainDofsBoundaryToFree = new ConcurrentDictionary<int, int[]>();
		private readonly ConcurrentDictionary<int, int[]> subdomainDofsInternalToFree = new ConcurrentDictionary<int, int[]>();
		private readonly ConcurrentDictionary<int, int> subdomainNumFreeDofs = new ConcurrentDictionary<int, int>();

		/// <summary>
		/// First key: current subdomain. Second key: neighbor subdomain. Value: dofs at common nodes between these 2 subdomains
		/// </summary>
		private readonly ConcurrentDictionary<int, Dictionary<int, DofSet>> commonDofsBetweenSubdomains
			= new ConcurrentDictionary<int, Dictionary<int, DofSet>>();

		public PsmDofSeparator(IComputeEnvironment environment, IStructuralModel model, SubdomainTopology subdomainTopology,
			bool sortDofsWhenPossible = false)
		{
			this.environment = environment;
			this.model = model;
			this.subdomainTopology = subdomainTopology;
			this.sortDofsWhenPossible = sortDofsWhenPossible;
		}

		public DistributedOverlappingIndexer CreateDistributedVectorIndexer()
		{
			var indexer = new DistributedOverlappingIndexer(environment);
			Action<int> initializeIndexer = subdomainID =>
			{
				ISubdomain subdomain = model.GetSubdomain(subdomainID);
				int numBoundaryDofs = subdomainDofsBoundaryToFree[subdomainID].Length;
				DofTable boundaryDofs = subdomainDofOrderingsBoundary[subdomainID];

				var allCommonDofIndices = new Dictionary<int, int[]>();
				foreach (int neighborID in subdomainTopology.GetNeighborsOfSubdomain(subdomainID))
				{
					DofSet commonDofs2 = commonDofsBetweenSubdomains[subdomainID][neighborID];
					var commonDofIndices = new int[commonDofs2.Count()];
					int idx = 0;
					foreach ((int nodeID, int dofID) in commonDofs2.EnumerateNodesDofs()) 
					{
						//TODO: It would be faster to iterate each node and then its dofs. Same for DofTable. 
						//		Even better let DofTable take DofSet as argument and return the indices.
						INode node = model.GetNode(nodeID);
						IDofType dof = AllDofs.GetDofWithId(dofID);
						commonDofIndices[idx++] = boundaryDofs[node, dof];
					}
					allCommonDofIndices[neighborID] = commonDofIndices;
				}

				indexer.GetLocalComponent(subdomainID).Initialize(numBoundaryDofs, allCommonDofIndices);
			};
			environment.DoPerNode(initializeIndexer);
			return indexer;
		}

		public void FindCommonDofsBetweenSubdomains()
		{
			// Find all dofs of each subdomain at the common nodes.
			Action<int> findLocalDofsAtCommonNodes = subdomainID =>
			{
				Dictionary<int, DofSet> commonDofs = FindSubdomainDofsAtCommonNodes(subdomainID);
				commonDofsBetweenSubdomains[subdomainID] = commonDofs;
			};
			environment.DoPerNode(findLocalDofsAtCommonNodes);

			// Send these dofs to the corresponding neighbors and receive theirs.
			Func<int, AllToAllNodeData<int>> prepareDofsToSend = subdomainID =>
			{
				var transferData = new AllToAllNodeData<int>();
				transferData.sendValues = new ConcurrentDictionary<int, int[]>();
				foreach (int neighborID in subdomainTopology.GetNeighborsOfSubdomain(subdomainID))
				{
					DofSet commonDofs = commonDofsBetweenSubdomains[subdomainID][neighborID];

					//TODOMPI: Serialization & deserialization should be done by the environment, if necessary.
					transferData.sendValues[neighborID] = commonDofs.Serialize(); 
				}

				// No buffers for receive values yet, since their lengths are unknown. 
				// Let the environment create them, by using extra communication.
				transferData.recvValues = new ConcurrentDictionary<int, int[]>();
				return transferData;
			};
			Dictionary<int, AllToAllNodeData<int>> transferDataPerSubdomain =
				environment.CreateDictionaryPerNode(prepareDofsToSend);
			environment.NeighborhoodAllToAll(transferDataPerSubdomain, false);

			// Find the intersection between the dofs of a subdomain and the ones received by its neighbor.
			Action<int> processReceivedDofs = subdomainID =>
			{
				AllToAllNodeData<int> transferData = transferDataPerSubdomain[subdomainID];
				foreach (int neighborID in subdomainTopology.GetNeighborsOfSubdomain(subdomainID))
				{
					DofSet receivedDofs = DofSet.Deserialize(transferData.recvValues[neighborID]);
					commonDofsBetweenSubdomains[subdomainID][neighborID] =
						commonDofsBetweenSubdomains[subdomainID][neighborID].IntersectionWith(receivedDofs);
				}
			};
			environment.DoPerNode(processReceivedDofs);
		}

		public DofTable GetSubdomainDofOrderingBoundary(int subdomainID) => subdomainDofOrderingsBoundary[subdomainID];

		public int[] GetSubdomainDofsBoundaryToFree(int subdomainID) => subdomainDofsBoundaryToFree[subdomainID];

		public int[] GetSubdomainDofsInternalToFree(int subdomainID) => subdomainDofsInternalToFree[subdomainID];

		public int GetNumSubdomainFreeDofs(int subdomainID) => subdomainNumFreeDofs[subdomainID];

		public void ReorderSubdomainInternalDofs(int subdomainID, DofPermutation permutation)
		{
			if (permutation.IsBetter)
			{
				int[] internalDofs = permutation.ReorderKeysOfDofIndicesMap(subdomainDofsInternalToFree[subdomainID]);
				subdomainDofsInternalToFree[subdomainID] = internalDofs; 
			}
		}

		/// <summary>
		/// Boundary/internal dofs
		/// </summary>
		public void SeparateSubdomainDofsIntoBoundaryInternal()
		{
			Action<int> subdomainAction = subdomainID =>
			{
				ISubdomain subdomain = model.GetSubdomain(subdomainID);
				(DofTable boundaryDofOrdering, int[] boundaryToFree, int[] internalToFree) = SeparateSubdomainDofs(subdomain);

				subdomainNumFreeDofs[subdomainID] = subdomain.FreeDofOrdering.NumFreeDofs;
				subdomainDofOrderingsBoundary[subdomainID] = boundaryDofOrdering;
				subdomainDofsBoundaryToFree[subdomainID] = boundaryToFree;
				subdomainDofsInternalToFree[subdomainID] = internalToFree;
			};
			environment.DoPerNode(subdomainAction);
		}

		private Dictionary<int, DofSet> FindSubdomainDofsAtCommonNodes(int subdomainID)
		{
			var commonDofsOfSubdomain = new Dictionary<int, DofSet>();
			ISubdomain subdomain = model.GetSubdomain(subdomainID);
			DofTable freeDofs = subdomain.FreeDofOrdering.FreeDofs;
			foreach (int neighborID in subdomainTopology.GetNeighborsOfSubdomain(subdomainID))
			{
				var dofSet = new DofSet();
				foreach (int nodeID in subdomainTopology.GetCommonNodesOfSubdomains(subdomainID, neighborID))
				{
					INode node = model.GetNode(nodeID);
					dofSet.AddDofs(node, freeDofs.GetColumnsOfRow(node));
				}
				commonDofsOfSubdomain[neighborID] = dofSet;
			}
			return commonDofsOfSubdomain;
		}

		private (DofTable boundaryDofOrdering, int[] boundaryToFree, int[] internalToFree) SeparateSubdomainDofs(
			ISubdomain subdomain)
		{
			//TODOMPI: force sorting per node and dof
			var boundaryDofOrdering = new DofTable();
			var boundaryToFree = new List<int>();
			var internalToFree = new HashSet<int>();
			int subdomainBoundaryIdx = 0;

			DofTable freeDofs = subdomain.FreeDofOrdering.FreeDofs;
			IEnumerable<INode> nodes = freeDofs.GetRows();
			if (sortDofsWhenPossible)
			{
				nodes = nodes.OrderBy(node => node.ID);
			}

			foreach (INode node in nodes) //TODO: Optimize access: Directly get INode, Dictionary<IDof, int>
			{
				IReadOnlyDictionary<IDofType, int> dofsOfNode = freeDofs.GetDataOfRow(node);
				if (sortDofsWhenPossible)
				{
					var sortedDofsOfNode = new SortedDictionary<IDofType, int>(new DofTypeComparer());
					foreach (var dofTypeIdxPair in dofsOfNode)
					{
						sortedDofsOfNode[dofTypeIdxPair.Key] = dofTypeIdxPair.Value;
					}
					dofsOfNode = sortedDofsOfNode;
				}

				if (node.SubdomainsDictionary.Count > 1)
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

		private class DofTypeComparer : IComparer<IDofType>
		{
			public int Compare(IDofType x, IDofType y)
			{
				return AllDofs.GetIdOfDof(x) - AllDofs.GetIdOfDof(y);
			}
		}
	}
}
