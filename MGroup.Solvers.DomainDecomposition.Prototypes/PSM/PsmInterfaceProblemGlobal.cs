using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Iterative;
using ISAAR.MSolve.LinearAlgebra.Iterative.PreconditionedConjugateGradient;
using ISAAR.MSolve.LinearAlgebra.Iterative.Preconditioning;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.DomainDecomposition.Prototypes.LinearAlgebraExtensions;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.PSM
{
	public class PsmInterfaceProblemGlobal : IPsmInterfaceProblem
	{
		private readonly IStructuralModel model;
		private readonly PsmSubdomainDofs dofs;

		public PsmInterfaceProblemGlobal(IStructuralModel model, PsmSubdomainDofs dofs)
		{
			this.model = model;
			this.dofs = dofs;
		}

		public DofTable GlobalDofOrderingBoundary { get; set; }

		public BlockMatrix MatrixLbe { get; set; }

		public int NumGlobalDofsBoundary { get; set; }

		public Dictionary<int, Matrix> SubdomainMatricesLb { get; } = new Dictionary<int, Matrix>();

		public void FindDofs()
		{
			FindGlobalBoundaryDofs();
			MapBoundaryDofsGlobalToSubdomains();
			CalcLbe();
		}

		public IterativeStatistics Solve(PcgAlgorithm pcg, IPreconditioner preconditioner,
			BlockMatrix expandedDomainMatrix, BlockVector expandedDomainRhs, BlockVector expandedDomainSolution)
		{
			// Interface problem
			Matrix fullLbe = MatrixLbe.CopyToFullMatrix();
			Matrix fullSbbe = expandedDomainMatrix.CopyToFullMatrix();
			Vector fullFbeCond = expandedDomainRhs.CopyToFullVector();
			Matrix interfaceMatrix = fullLbe.Transpose() * fullSbbe * fullLbe;
			Vector interfaceRhs = fullLbe.Transpose() * fullFbeCond;
			var interfaceSolution = Vector.CreateZero(interfaceRhs.Length);

			// Interface problem solution using CG
			var pcgBuilder = new PcgAlgorithm.Builder();
			var stats = pcg.Solve(interfaceMatrix, preconditioner, interfaceRhs, interfaceSolution, false,
				() => Vector.CreateZero(interfaceRhs.Length));

			expandedDomainSolution.CopyFrom(MatrixLbe * interfaceSolution);

			return stats;
		}

		private void CalcLbe()
		{
			MatrixLbe = BlockMatrix.CreateCol(dofs.NumSubdomainDofsBoundary, NumGlobalDofsBoundary);
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				int s = subdomain.ID;
				MatrixLbe.AddBlock(s, 0, SubdomainMatricesLb[s]);
			}
		}

		private void FindGlobalBoundaryDofs()
		{
			var globalBoundaryDofs = new SortedDofTable();
			int numBoundaryDofs = 0;
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				foreach ((INode node, IDofType dof, int idx) in dofs.SubdomainDofOrderingBoundary[subdomain.ID])
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

		private void MapBoundaryDofsGlobalToSubdomains()
		{
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				DofTable subdomainDofs = dofs.SubdomainDofOrderingBoundary[subdomain.ID];
				var Lb = Matrix.CreateZero(dofs.NumSubdomainDofsBoundary[subdomain.ID], NumGlobalDofsBoundary);
				foreach ((INode node, IDofType dof, int subdomainIdx) in subdomainDofs)
				{
					int globalIdx = GlobalDofOrderingBoundary[node, dof];
					Lb[subdomainIdx, globalIdx] = 1.0;
				}
				SubdomainMatricesLb[subdomain.ID] = Lb;
			}
		}
	}
}
