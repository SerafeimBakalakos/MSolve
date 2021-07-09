using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.FetiDP
{
    public class FetiDPDofs
    {
        protected readonly IStructuralModel model;

        public FetiDPDofs(IStructuralModel model)
        {
            this.model = model;
        }

        public DofTable GlobalDofOrderingCorner { get; set; }

        public int NumGlobalDofsCorner { get; set; }

        public Dictionary<int, int> NumSubdomainDofsCorner { get; } = new Dictionary<int, int>();

        public Dictionary<int, int> NumSubdomainDofsRemainder { get; } = new Dictionary<int, int>();

        public Dictionary<int, DofTable> SubdomainDofOrderingCorner { get; } = new Dictionary<int, DofTable>();

        public Dictionary<int, int[]> SubdomainDofsCornerToFree { get; } = new Dictionary<int, int[]>();

        public Dictionary<int, int[]> SubdomainDofsRemainderToFree { get; } = new Dictionary<int, int[]>();

		public Dictionary<int, Matrix> SubdomainMatricesLc { get; } = new Dictionary<int, Matrix>();

		public virtual void FindDofs(ICornerDofSelection cornerDofs)
		{
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				SeparateFreeDofsIntoCornerAndRemainder(subdomain, cornerDofs);
			}

			FindGlobalCornerDofs(cornerDofs);
			MapCornerDofsGlobalToSubdomains();
		}

		protected void FindGlobalCornerDofs(ICornerDofSelection cornerDofs)
		{
			var globalCornerDofs = new SortedDofTable();
			int numCornerDofs = 0;
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				foreach ((INode node, IDofType dof, int idx) in SubdomainDofOrderingCorner[subdomain.ID])
				{
					bool didNotExist = globalCornerDofs.TryAdd(node.ID, AllDofs.GetIdOfDof(dof), numCornerDofs);
					if (didNotExist)
					{
						numCornerDofs++;
					}
				}
			}

			var cornerDofOrdering = new DofTable();
			foreach ((int nodeID, int dofID, int idx) in globalCornerDofs)
			{
				cornerDofOrdering[model.GetNode(nodeID), AllDofs.GetDofWithId(dofID)] = idx;
			}

			GlobalDofOrderingCorner = cornerDofOrdering;
			NumGlobalDofsCorner = numCornerDofs;
		}

		protected void MapCornerDofsGlobalToSubdomains()
		{
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				DofTable subdomainDofs = SubdomainDofOrderingCorner[subdomain.ID];
				var Lc = Matrix.CreateZero(NumSubdomainDofsCorner[subdomain.ID], NumGlobalDofsCorner);
				foreach ((INode node, IDofType dof, int subdomainIdx) in subdomainDofs)
				{
					int globalIdx = GlobalDofOrderingCorner[node, dof];
					Lc[subdomainIdx, globalIdx] = 1.0;
				}
				SubdomainMatricesLc[subdomain.ID] = Lc;
			}
		}

		protected void SeparateFreeDofsIntoCornerAndRemainder(ISubdomain subdomain, ICornerDofSelection cornerDofs)
		{
			var cornerDofOrdering = new DofTable();
			var cornerToFree = new List<int>();
			var remainderToFree = new HashSet<int>();
			int subdomainCornerIdx = 0;

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

				foreach (var pair in dofsOfNode)
				{
					IDofType dof = pair.Key;
					int freeDofIdx = pair.Value;
					if (cornerDofs.IsCornerDof(node, dof))
					{
						cornerDofOrdering[node, dof] = subdomainCornerIdx++;
						cornerToFree.Add(freeDofIdx);
					}
					else
					{
						remainderToFree.Add(freeDofIdx);
					}
				}
			}

			int s = subdomain.ID;
			SubdomainDofOrderingCorner[s] = cornerDofOrdering;
			NumSubdomainDofsCorner[s] = cornerToFree.Count;
			NumSubdomainDofsRemainder[s] = remainderToFree.Count;
			SubdomainDofsCornerToFree[s] = cornerToFree.ToArray();
			SubdomainDofsRemainderToFree[s] = remainderToFree.ToArray();
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
