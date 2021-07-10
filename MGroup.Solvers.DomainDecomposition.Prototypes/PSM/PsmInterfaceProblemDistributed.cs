using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Iterative;
using ISAAR.MSolve.LinearAlgebra.Iterative.PreconditionedConjugateGradient;
using ISAAR.MSolve.LinearAlgebra.Iterative.Preconditioning;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using MGroup.Solvers.DomainDecomposition.Prototypes.LinearAlgebraExtensions;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.PSM
{
	public class PsmInterfaceProblemDistributed : IPsmInterfaceProblem
	{
		public PsmInterfaceProblemDistributed()
		{
		}

		public PsmSubdomainDofs Dofs { get; set; }

		public BlockMatrix MatrixMbe { get; set; }

		public IStructuralModel Model { get; set; }

		public Dictionary<int, int[]> SubdomainBoundaryDofMultiplicities { get; } = new Dictionary<int, int[]>();

		public Dictionary<int, Dictionary<int, Matrix>> SubdomainMatricesMb { get; }
			= new Dictionary<int, Dictionary<int, Matrix>>();

		public void CalcMappingMatrices()
		{
			MatrixMbe = BlockMatrix.Create(Dofs.NumSubdomainDofsBoundary, Dofs.NumSubdomainDofsBoundary);
			foreach (ISubdomain subdomain in Model.Subdomains)
			{
				SubdomainBoundaryDofMultiplicities[subdomain.ID] = FindBoundaryDofMultiplicities(subdomain);

				var subdomainMb = new Dictionary<int, Matrix>();
				SubdomainMatricesMb[subdomain.ID] = subdomainMb;
				foreach (ISubdomain other in Model.Subdomains)
				{
					Matrix Mb = MapInterSubdomainDofs(subdomain, other);
					subdomainMb[other.ID] = Mb;
					MatrixMbe.AddBlock(subdomain.ID, other.ID, Mb);
				}
			}
		}

		public IterativeStatistics Solve(PcgAlgorithm pcg, IPreconditioner preconditioner,
			BlockMatrix expandedDomainMatrix, BlockVector expandedDomainRhs, BlockVector expandedDomainSolution)
		{
			int[][] multiplicities = CalcMultiplicities();
			MatrixMbe.RowMultiplicities = multiplicities;
			MatrixMbe.ColMultiplicities = multiplicities;
			expandedDomainMatrix.RowMultiplicities = multiplicities;
			expandedDomainMatrix.ColMultiplicities = multiplicities;
			expandedDomainRhs.Multiplicities = multiplicities;
			expandedDomainSolution.Multiplicities = multiplicities;

			// Interface problem
			var interfaceMatrix = new ChainVectorMultipliable(MatrixMbe, expandedDomainMatrix);
			BlockVector interfaceRhs = MatrixMbe * expandedDomainRhs;

			// Interface problem solution using CG
			var stats = pcg.Solve(interfaceMatrix, preconditioner, interfaceRhs, expandedDomainSolution, false,
				expandedDomainSolution.CreateZeroVectorWithSameFormat);

			return stats;
		}

		private int[] FindBoundaryDofMultiplicities(ISubdomain subdomain)
		{
			var result = new int[Dofs.NumSubdomainDofsBoundary[subdomain.ID]];
			foreach ((INode node, IDofType dof, int idx) in Dofs.SubdomainDofOrderingBoundary[subdomain.ID])
			{
				result[idx] = node.SubdomainsDictionary.Count;
			}
			return result;
		}

		private Matrix MapInterSubdomainDofs(ISubdomain rowSubdomain, ISubdomain colSubdomain)
		{
			int sR = rowSubdomain.ID;
			int sC = colSubdomain.ID;
			var result = Matrix.CreateZero(Dofs.NumSubdomainDofsBoundary[sR], Dofs.NumSubdomainDofsBoundary[sC]);
			DofTable rowBoundaryDofs = Dofs.SubdomainDofOrderingBoundary[sR];
			DofTable colBoundaryDofs = Dofs.SubdomainDofOrderingBoundary[sC];
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

		private int[][] CalcMultiplicities()
		{
			var multiplicities = new int[Model.Subdomains.Count][];
			foreach (ISubdomain subdomain in Model.Subdomains)
			{
				int s = subdomain.ID;
				multiplicities[s] = SubdomainBoundaryDofMultiplicities[s];
			}
			return multiplicities;
		}
	}
}
