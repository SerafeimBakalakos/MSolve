﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.DomainDecomposition.Commons;

namespace MGroup.Solvers.DomainDecomposition.FetiDP.Dofs
{
    public class FetiDPSubdomainDofs
    {
		public FetiDPSubdomainDofs(ISubdomain subdomain)
		{
			this.Subdomain = subdomain;
		}

		public DofTable DofOrderingCorner { get; private set; }

		public int[] DofsBoundaryRemainderToRemainder { get; private set; }

		public int[] DofsCornerToFree { get; private set; }

		public int[] DofsInternalToRemainder { get; private set; }

		public int[] DofsRemainderToFree { get; private set; }

		public int NumFreeDofs { get; private set; }

		public ISubdomain Subdomain { get; }

		public void ReorderRemainderDofs(DofPermutation permutation)
		{
			if (permutation.IsBetter)
			{
				DofsRemainderToFree = permutation.ReorderKeysOfDofIndicesMap(DofsRemainderToFree);
			}
		}

		public void SeparateFreeDofsIntoCornerAndRemainder(ICornerDofSelection cornerDofSelection)
		{
			var cornerDofOrdering = new DofTable();
			var cornerToFree = new List<int>();
			var remainderToFree = new HashSet<int>();
			int numCornerDofs = 0;
			DofTable freeDofs = Subdomain.FreeDofOrdering.FreeDofs;
			IEnumerable<INode> nodes = freeDofs.GetRows(); //TODO: Optimize access: Directly get INode, Dictionary<IDof, int>
			foreach (INode node in nodes)
			{
				IReadOnlyDictionary<IDofType, int> dofsOfNode = freeDofs.GetDataOfRow(node);
				foreach (var dofIdxPair in dofsOfNode)
				{
					IDofType dof = dofIdxPair.Key;
					if (cornerDofSelection.IsCornerDof(node, dof))
					{
						cornerDofOrdering[node, dof] = numCornerDofs++;
						cornerToFree.Add(dofIdxPair.Value);
					}
					else
					{
						remainderToFree.Add(dofIdxPair.Value);
					}
				}
			}

			this.NumFreeDofs = Subdomain.FreeDofOrdering.NumFreeDofs;
			this.DofOrderingCorner = cornerDofOrdering;
			this.DofsCornerToFree = cornerToFree.ToArray();
			this.DofsRemainderToFree = remainderToFree.ToArray();
		}

		public void SeparateRemainderDofsIntoBoundaryAndInternalDofs()
		{
			throw new NotImplementedException("Useful in FETI-DP, but not in P-FETI-DP");
		}
	}
}