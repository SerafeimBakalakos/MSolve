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
		public PsmInterfaceProblemGlobal()
		{
		}

		public PsmSubdomainDofs Dofs { get; set; }

		public DofTable GlobalDofOrderingBoundary { get; set; }

		public BlockMatrix MatrixLbe { get; set; }

		public IStructuralModel Model { get; set; }

		public int NumGlobalDofsBoundary { get; set; }

		public Dictionary<int, Matrix> SubdomainMatricesLb { get; } = new Dictionary<int, Matrix>();

		public void CalcMappingMatrices()
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
			MatrixLbe = BlockMatrix.CreateCol(Dofs.NumSubdomainDofsBoundary, NumGlobalDofsBoundary);
			foreach (ISubdomain subdomain in Model.Subdomains)
			{
				int s = subdomain.ID;
				MatrixLbe.AddBlock(s, 0, SubdomainMatricesLb[s]);
			}
		}

		private void FindGlobalBoundaryDofs()
		{
			var globalBoundaryDofs = new SortedDofTable();
			int numBoundaryDofs = 0;
			foreach (ISubdomain subdomain in Model.Subdomains)
			{
				foreach ((INode node, IDofType dof, int idx) in Dofs.SubdomainDofOrderingBoundary[subdomain.ID])
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
				boundaryDofOrdering[Model.GetNode(nodeID), AllDofs.GetDofWithId(dofID)] = idx;
			}

			GlobalDofOrderingBoundary = boundaryDofOrdering;
			NumGlobalDofsBoundary = numBoundaryDofs;
		}

		private void MapBoundaryDofsGlobalToSubdomains()
		{
			foreach (ISubdomain subdomain in Model.Subdomains)
			{
				DofTable subdomainDofs = Dofs.SubdomainDofOrderingBoundary[subdomain.ID];
				var Lb = Matrix.CreateZero(Dofs.NumSubdomainDofsBoundary[subdomain.ID], NumGlobalDofsBoundary);
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
