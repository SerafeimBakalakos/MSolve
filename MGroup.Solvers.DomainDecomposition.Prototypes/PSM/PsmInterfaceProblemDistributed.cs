﻿using System;
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
		private readonly IStructuralModel model;
		private readonly PsmSubdomainDofs dofs;

		public PsmInterfaceProblemDistributed(IStructuralModel model, PsmSubdomainDofs dofs)
		{
			this.model = model;
			this.dofs = dofs;
		}

		public BlockMatrix MatrixMbe { get; set; }

		public Dictionary<int, int[]> SubdomainBoundaryDofMultiplicities { get; } = new Dictionary<int, int[]>();

		public Dictionary<int, Dictionary<int, Matrix>> SubdomainMatricesMb { get; }
			= new Dictionary<int, Dictionary<int, Matrix>>();

		public void FindDofs()
		{
			MatrixMbe = BlockMatrix.Create(dofs.NumSubdomainDofsBoundary, dofs.NumSubdomainDofsBoundary);
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				SubdomainBoundaryDofMultiplicities[subdomain.ID] = FindBoundaryDofMultiplicities(subdomain);

				var subdomainMb = new Dictionary<int, Matrix>();
				SubdomainMatricesMb[subdomain.ID] = subdomainMb;
				foreach (ISubdomain other in model.Subdomains)
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
			var result = new int[dofs.NumSubdomainDofsBoundary[subdomain.ID]];
			foreach ((INode node, IDofType dof, int idx) in dofs.SubdomainDofOrderingBoundary[subdomain.ID])
			{
				result[idx] = node.SubdomainsDictionary.Count;
			}
			return result;
		}

		private Matrix MapInterSubdomainDofs(ISubdomain rowSubdomain, ISubdomain colSubdomain)
		{
			int sR = rowSubdomain.ID;
			int sC = colSubdomain.ID;
			var result = Matrix.CreateZero(dofs.NumSubdomainDofsBoundary[sR], dofs.NumSubdomainDofsBoundary[sC]);
			DofTable rowBoundaryDofs = dofs.SubdomainDofOrderingBoundary[sR];
			DofTable colBoundaryDofs = dofs.SubdomainDofOrderingBoundary[sC];
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
			var multiplicities = new int[model.Subdomains.Count][];
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				int s = subdomain.ID;
				multiplicities[s] = SubdomainBoundaryDofMultiplicities[s];
			}
			return multiplicities;
		}
	}
}