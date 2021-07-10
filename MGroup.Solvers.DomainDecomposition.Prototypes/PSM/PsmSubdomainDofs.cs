using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using MGroup.Solvers;

namespace MGroup.Solvers.DomainDecomposition.Prototypes
{
	public class PsmSubdomainDofs
	{
		protected readonly IStructuralModel model;

		public PsmSubdomainDofs(IStructuralModel model)
		{
			this.model = model;
		}

		public Dictionary<int, int> NumSubdomainDofsBoundary { get; } = new Dictionary<int, int>();

		public Dictionary<int, int> NumSubdomainDofsFree { get; } = new Dictionary<int, int>();

		public Dictionary<int, int> NumSubdomainDofsInternal { get; } = new Dictionary<int, int>();

		public Dictionary<int, DofTable> SubdomainDofOrderingBoundary { get; } = new Dictionary<int, DofTable>();

		public Dictionary<int, int[]> SubdomainDofsBoundaryToFree { get; } = new Dictionary<int, int[]>();

		public Dictionary<int, int[]> SubdomainDofsInternalToFree { get; } = new Dictionary<int, int[]>();

		public void FindDofs()
		{
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				SeparateFreeDofsIntoBoundaryAndInternal(subdomain);
			}
		}

		protected void SeparateFreeDofsIntoBoundaryAndInternal(ISubdomain subdomain)
		{
			var boundaryDofOrdering = new DofTable();
			var boundaryToFree = new List<int>();
			var internalToFree = new HashSet<int>();
			int subdomainBoundaryIdx = 0;

			DofTable freeDofs = subdomain.FreeDofOrdering.FreeDofs;
			IEnumerable<INode> nodes = freeDofs.GetRows();
			nodes = nodes.OrderBy(node => node.ID);

			foreach (INode node in nodes)
			{
				IReadOnlyDictionary<IDofType, int> dofsOfNode = freeDofs.GetDataOfRow(node);
				var sortedDofsOfNode = new SortedDictionary<IDofType, int>(new DofTypeComparer());
				foreach (var dofTypeIdxPair in dofsOfNode)
				{
					sortedDofsOfNode[dofTypeIdxPair.Key] = dofTypeIdxPair.Value;
				}
				dofsOfNode = sortedDofsOfNode;

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

			int s = subdomain.ID;
			SubdomainDofOrderingBoundary[s] = boundaryDofOrdering;
			NumSubdomainDofsBoundary[s] = boundaryToFree.Count;
			NumSubdomainDofsInternal[s] = internalToFree.Count;
			NumSubdomainDofsFree[s] = subdomain.FreeDofOrdering.NumFreeDofs;
			SubdomainDofsBoundaryToFree[s] = boundaryToFree.ToArray();
			SubdomainDofsInternalToFree[s] = internalToFree.ToArray();
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
