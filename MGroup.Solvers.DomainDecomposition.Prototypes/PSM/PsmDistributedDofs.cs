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
	public class PsmDistributedDofs
	{
		private readonly IStructuralModel model;
		private readonly PsmDofs psmDofs;

		public PsmDistributedDofs(IStructuralModel model, PsmDofs psmDofs)
		{
			this.model = model;
			this.psmDofs = psmDofs;
		}

		public Dictionary<int, int[]> SubdomainBoundaryDofMultiplicities { get; } = new Dictionary<int, int[]>();

		public Dictionary<int, Dictionary<int, Matrix>> SubdomainMatricesMb { get; } 
			= new Dictionary<int, Dictionary<int, Matrix>>();

		public void Prepare()
		{
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				SubdomainBoundaryDofMultiplicities[subdomain.ID] = FindBoundaryDofMultiplicities(subdomain);

				var subdomainMb = new Dictionary<int, Matrix>();
				SubdomainMatricesMb[subdomain.ID] = subdomainMb;
				foreach (ISubdomain other in model.Subdomains)
				{
					subdomainMb[other.ID] = MapInterSubdomainDofs(subdomain, other);
				}
			}
		}

		private int[] FindBoundaryDofMultiplicities(ISubdomain subdomain)
		{
			var result = new int[psmDofs.NumSubdomainDofsBoundary[subdomain.ID]];
			foreach ((INode node, IDofType dof, int idx) in psmDofs.SubdomainDofOrderingBoundary[subdomain.ID])
			{
				result[idx] = node.SubdomainsDictionary.Count;
			}
			return result;
		}

		private Matrix MapInterSubdomainDofs(ISubdomain rowSubdomain, ISubdomain colSubdomain)
		{
			int sR = rowSubdomain.ID;
			int sC = colSubdomain.ID;
			var result = Matrix.CreateZero(psmDofs.NumSubdomainDofsBoundary[sR], psmDofs.NumSubdomainDofsBoundary[sC]);
			DofTable rowBoundaryDofs = psmDofs.SubdomainDofOrderingBoundary[sR];
			DofTable colBoundaryDofs = psmDofs.SubdomainDofOrderingBoundary[sC];
			foreach ((INode node, IDofType dof, int row) in rowBoundaryDofs)
			{
				bool isCommonDof = colBoundaryDofs.TryGetValue(node, dof, out int col);
				if (isCommonDof)
				{
					result[row, col] = 1.0;
				}
			}
			return result;
		}
	}
}
