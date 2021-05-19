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
using MGroup.Solvers_OLD.DDM.Environments;
using MGroup.Solvers_OLD.DDM.FetiDP.Dofs;
using MGroup.Solvers_OLD.DDM.Mappings;
using MGroup.Solvers_OLD.Commons;

namespace MGroup.Solvers_OLD.DDM.FetiDP.CoarseProblem
{
	public class FetiDPCoarseProblemCSparseNonSymmetric : IFetiDPCoarseProblem
	{
		private readonly IDdmEnvironment environment;
		private readonly IStructuralModel model;
		private LUCSparseNet inverseGlobalKccStar;

		public FetiDPCoarseProblemCSparseNonSymmetric(IDdmEnvironment environment, IStructuralModel model)
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
			var globalKccStar = DokColMajor.CreateEmpty(numGlobalCornerDofs, numGlobalCornerDofs);

			// Static condensation of remainder dofs (Schur complement).
			foreach (int s in subdomainKccStar.Keys)
			{
				// globalKccStar = sum_over_s(Lc[s]^T * KccStar[s] * Lc[s])
				int[] subdomainDofs = Utilities.Range(0, subdomainLc[s].NumRows);
				int[] globalDofs = subdomainLc[s].RowsToColumns;
				globalKccStar.AddSubmatrix(subdomainKccStar[s], subdomainDofs, globalDofs, subdomainDofs, globalDofs);
			}

			CscMatrix temp = globalKccStar.BuildCscMatrix(true);
			this.inverseGlobalKccStar = LUCSparseNet.Factorize(temp);
		}

		public Vector MultiplyInverseCoarseProblemMatrixTimes(Vector vector) => inverseGlobalKccStar.SolveLinearSystem(vector);

		public void ReorderGlobalCornerDofs(IFetiDPDofSeparator dofSeparator)
		{
			// Do nothing, since the sparsity pattern is irrelevant for dense matrices.
		}

		public class Factory : IFetiDPCoarseProblemFactory
		{
			public IFetiDPCoarseProblem Create(IDdmEnvironment environment, IStructuralModel model)
				=> new FetiDPCoarseProblemCSparseNonSymmetric(environment, model);
		}
	}
}
