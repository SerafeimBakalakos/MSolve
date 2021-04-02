using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Builders;
using ISAAR.MSolve.LinearAlgebra.Reordering;
using ISAAR.MSolve.LinearAlgebra.Triangulation;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.DDM.Environments;
using MGroup.Solvers.DDM.FetiDP.Dofs;
using MGroup.Solvers.DDM.Mappings;
using MGroup.Solvers.DofOrdering.Reordering;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using MGroup.Solvers.Commons;

namespace MGroup.Solvers.DDM.FetiDP.CoarseProblem
{
	public class FetiDPCoarseProblemCSparse : IFetiDPCoarseProblem
	{
		private readonly IDdmEnvironment environment;
		private readonly IStructuralModel model;
		private CholeskyCSparseNet inverseGlobalKccStar;
		private readonly OrderingAmdCSparseNet reordering = new OrderingAmdCSparseNet();

		public FetiDPCoarseProblemCSparse(IDdmEnvironment environment, IStructuralModel model)
		{
			this.environment = environment;
			this.model = model;
		}

		public void ClearCoarseProblemMatrix()
		{
			inverseGlobalKccStar = null;
		}

		public void CreateAndInvertCoarseProblemMatrix(Dictionary<int, BooleanMatrixRowsToColumns> subdomainLc,
			Dictionary<int, IMatrix> subdomainKccStar)
		{
			int numGlobalCornerDofs = subdomainLc.First().Value.NumColumns;
			var globalKccStar = DokSymmetric.CreateEmpty(numGlobalCornerDofs);

			// Static condensation of remainder dofs (Schur complement).
			foreach (int s in subdomainKccStar.Keys)
			{
				// globalKccStar = sum_over_s(Lc[s]^T * KccStar[s] * Lc[s])
				int[] subdomainDofs = Utilities.Range(0, subdomainLc[s].NumRows); //TODO: Create a DokSymmetric.AddSubmatrixSymmetric() overload that accepts a single mapping array
				int[] globalDofs = subdomainLc[s].RowsToColumns;
				globalKccStar.AddSubmatrixSymmetric(subdomainKccStar[s], subdomainDofs, globalDofs);
			}

			SymmetricCscMatrix temp = globalKccStar.BuildSymmetricCscMatrix(true);
			this.inverseGlobalKccStar = CholeskyCSparseNet.Factorize(temp);
		}

		public Vector MultiplyInverseCoarseProblemMatrixTimes(Vector vector) => inverseGlobalKccStar.SolveLinearSystem(vector);

		public void ReorderGlobalCornerDofs(IFetiDPDofSeparator dofSeparator)
		{
			var pattern = SparsityPatternSymmetric.CreateEmpty(dofSeparator.NumGlobalCornerDofs);
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				// Treat each subdomain as a superelement with only its corner nodes.
				DofTable subdomainCornerDofOrdering = dofSeparator.GetDofOrderingCorner(subdomain.ID);
				int numSubdomainCornerDofs = dofSeparator.GetDofsCornerToFree(subdomain.ID).Length;
				var subdomainToGlobalDofs = new int[numSubdomainCornerDofs];
				foreach ((INode node, IDofType dofType, int subdomainIdx) in subdomainCornerDofOrdering)
				{
					int globalIdx = dofSeparator.GlobalCornerDofOrdering[node, dofType];
					subdomainToGlobalDofs[subdomainIdx] = globalIdx;
				}
				pattern.ConnectIndices(subdomainToGlobalDofs, false);
			}
			(int[] permutation, bool oldToNew) = reordering.FindPermutation(pattern);
			dofSeparator.ReorderGlobalCornerDofs(DofPermutation.Create(permutation, oldToNew));
		}

		public class Factory : IFetiDPCoarseProblemFactory
		{
			public IFetiDPCoarseProblem Create(IDdmEnvironment environment, IStructuralModel model)
				=> new FetiDPCoarseProblemCSparse(environment, model);
		}
	}
}
