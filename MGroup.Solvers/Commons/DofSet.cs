using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.DDM.Dofs;

namespace MGroup.Solvers.Commons
{
    public class DofSet
    {
		//TODO: Perhaps I should use HashSets and order them, when I actually need to number the dofs
		private readonly SortedDictionary<int, SortedSet<int>> data = new SortedDictionary<int, SortedSet<int>>();

		public void AddDof(INode node, IDofType dof) => AddDof(node.ID, AllDofs.GetIdOfDof(dof));

		public void AddDof(int nodeID, int dofID)
		{
			bool nodeExists = data.TryGetValue(nodeID, out SortedSet<int> dofsOfThisNode);
			if (!nodeExists)
			{
				dofsOfThisNode = new SortedSet<int>();
				data[nodeID] = dofsOfThisNode;
			}
			dofsOfThisNode.Add(dofID);
		}

		public void AddDofs(INode node, IEnumerable<IDofType> dofs) 
			=> AddDofs(node.ID, dofs.Select(dof => AllDofs.GetIdOfDof(dof)));

		public void AddDofs(int nodeID, IEnumerable<int> dofIDs)
		{
			bool nodeExists = data.TryGetValue(nodeID, out SortedSet<int> dofsOfThisNode);
			if (!nodeExists)
			{
				dofsOfThisNode = new SortedSet<int>();
				data[nodeID] = dofsOfThisNode;
			}
			dofsOfThisNode.UnionWith(dofIDs);
		}

		public IEnumerable<(int, int)> EnumerateNodesDofs()
		{
			foreach (var nodeDofPair in data)
			{
				int nodeID = nodeDofPair.Key;
				foreach (int dofID in nodeDofPair.Value)
				{
					yield return (nodeID, dofID);
				}
			}
		}

		public IEnumerable<int> GetDofsOfNode(int nodeID) => data[nodeID];

		//TODO: Use byte encodings. For dofs, int is wasteful. Short is better
		public int[] PackDofsOfNodes(IEnumerable<int> nodeSubset) 
		{
			var list = new List<int>();
			foreach (int nodeID in nodeSubset)
			{
				SortedSet<int> dofIDs = data[nodeID];
				list.Add(nodeID);
				list.Add(dofIDs.Count);
				list.AddRange(dofIDs);
			}
			return list.ToArray();
		}

		public (int numDofs, DofTable dofOrdering) OrderDofs(Func<int, INode> getNodeFromID) 
		{
			var dofTable = new DofTable();
			int numDofs = 0;
			foreach (var nodeDofPair in data)
			{
				INode node = getNodeFromID(nodeDofPair.Key);
				foreach (int dofID in nodeDofPair.Value)
				{
					IDofType dof = AllDofs.GetDofWithId(dofID);
					dofTable[node, dof] = numDofs++;
				}
			}
			return (numDofs, dofTable);
		}

		public void UnpackDofsAndUnion(int[] packedDofs)
		{
			int i = 0;
			while (i < packedDofs.Length)
			{
				int nodeID = packedDofs[i];
				Debug.Assert(i + 1 < packedDofs.Length, $"Node {nodeID} has no dofs listed.");

				int numDofs = packedDofs[i + 1];
				Debug.Assert(i + 1 + numDofs < packedDofs.Length, $"Node {nodeID} declared more dofs than actual.");

				SortedSet<int> dofsOfThisNode = data[nodeID];
				int offset = i + 2;
				for (int j = 0; j < numDofs; ++j)
				{
					dofsOfThisNode.Add(packedDofs[offset + j]);
				}

				i += 2 + numDofs;
			}
		}
	}
}
