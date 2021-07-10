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
    public class PsmDofs_OLD
    {
        protected readonly IStructuralModel model;

        public PsmDofs_OLD(IStructuralModel model)
        {
            this.model = model;
        }

		public DofTable GlobalDofOrderingBoundary { get; set; }

		public int NumGlobalDofsBoundary { get; set; }

		public Dictionary<int, int> NumSubdomainDofsBoundary { get; } = new Dictionary<int, int>();

		public Dictionary<int, int> NumSubdomainDofsFree { get; } = new Dictionary<int, int>();

		public Dictionary<int, int> NumSubdomainDofsInternal { get; } = new Dictionary<int, int>();

		public Dictionary<int, DofTable> SubdomainDofOrderingBoundary { get; } = new Dictionary<int, DofTable>();

		public Dictionary<int, int[]> SubdomainDofsBoundaryToFree { get; } = new Dictionary<int, int[]>();

        public Dictionary<int, int[]> SubdomainDofsInternalToFree { get; } = new Dictionary<int, int[]>();

		public Dictionary<int, Matrix> SubdomainMatricesLb { get; } = new Dictionary<int, Matrix>();

		public virtual void FindDofs()
        {
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				SeparateFreeDofsIntoBoundaryAndInternal(subdomain);
			}

			FindGlobalBoundaryDofs();
			MapBoundaryDofsGlobalToSubdomains();
		}

		protected void FindGlobalBoundaryDofs()
		{
			var globalBoundaryDofs = new SortedDofTable();
			int numBoundaryDofs = 0;
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				foreach ((INode node, IDofType dof, int idx) in SubdomainDofOrderingBoundary[subdomain.ID])
				{
					bool didNotExist = globalBoundaryDofs.TryAdd(node.ID, AllDofs.GetIdOfDof(dof), numBoundaryDofs);
					if (didNotExist)
					{
						numBoundaryDofs++;
					}
				}
			}

			var boundaryDofOrdering = new DofTable();
			foreach ((int nodeID, int dofID, int idx) in globalBoundaryDofs)
			{
				boundaryDofOrdering[model.GetNode(nodeID), AllDofs.GetDofWithId(dofID)] = idx;
			}

			GlobalDofOrderingBoundary = boundaryDofOrdering;
			NumGlobalDofsBoundary = numBoundaryDofs;
		}

		protected void MapBoundaryDofsGlobalToSubdomains()
		{
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				DofTable subdomainDofs = SubdomainDofOrderingBoundary[subdomain.ID];
				var Lb = Matrix.CreateZero(NumSubdomainDofsBoundary[subdomain.ID], NumGlobalDofsBoundary);
				foreach ((INode node, IDofType dof, int subdomainIdx) in subdomainDofs)
				{
					int globalIdx = GlobalDofOrderingBoundary[node, dof];
					Lb[subdomainIdx, globalIdx] = 1.0;
				}
				SubdomainMatricesLb[subdomain.ID] = Lb;
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
