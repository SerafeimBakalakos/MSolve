using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Builders;
using ISAAR.MSolve.LinearAlgebra.Reordering;
using ISAAR.MSolve.LinearAlgebra.Triangulation;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.DDM.Psm.Dofs;
using MGroup.Solvers.DDM.StiffnessMatrices;
using MGroup.Solvers.DofOrdering.Reordering;
using MGroup.Solvers.LinearAlgebraExtensions;

namespace MGroup.Solvers.DDM.Psm.StiffnessMatrices
{
	public class PsmMatrixManagerSuiteSparse : IPsmMatrixManager
	{
		private readonly IPsmDofSeparator dofSeparator;
		private readonly MatrixManagerCscSymmetric managerBasic;
		private readonly OrderingAmdSuiteSparse reordering = new OrderingAmdSuiteSparse();

		private readonly Dictionary<int, SubmatrixExtractorCsrCscSym> extractors = new Dictionary<int, SubmatrixExtractorCsrCscSym>();
		private Dictionary<int, CsrMatrix> Kbb = new Dictionary<int, CsrMatrix>();
		private Dictionary<int, CsrMatrix> Kbi = new Dictionary<int, CsrMatrix>();
		private Dictionary<int, SymmetricCscMatrix> Kii = new Dictionary<int, SymmetricCscMatrix>();
		private Dictionary<int, CholeskySuiteSparse> invKii = new Dictionary<int, CholeskySuiteSparse>();

		public PsmMatrixManagerSuiteSparse(IStructuralModel model, IPsmDofSeparator dofSeparator, MatrixManagerCscSymmetric managerBasic)
		{
			this.dofSeparator = dofSeparator;
			this.managerBasic = managerBasic;
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				extractors[subdomain.ID] = new SubmatrixExtractorCsrCscSym();
			}
		}

		public void ClearSubMatrices(int subdomainID)
		{
			lock (Kbb) Kbb[subdomainID] = null;
			lock (Kbi) Kbi[subdomainID] = null;
			lock (Kii) Kii[subdomainID] = null;
			if (invKii.ContainsKey(subdomainID))
			{
				invKii[subdomainID].Dispose();
			}
			lock (invKii) invKii[subdomainID] = null;
		}

		//TODO: Optimize this method. It is too slow.
		public void ExtractKiiKbbKib(int subdomainID)
		{
			int[] boundaryDofs = dofSeparator.GetDofsBoundaryToFree(subdomainID);
			int[] internalDofs = dofSeparator.GetDofsInternalToFree(subdomainID);

			SymmetricCscMatrix Kff = managerBasic.GetMatrixKff(subdomainID);
			extractors[subdomainID].ExtractSubmatrices(Kff, boundaryDofs, internalDofs);
			lock (Kbb) Kbb[subdomainID] = extractors[subdomainID].Submatrix00;
			lock (Kbi) Kbi[subdomainID] = extractors[subdomainID].Submatrix01;
			lock (Kii) Kii[subdomainID] = extractors[subdomainID].Submatrix11;
		}

		public void InvertKii(int subdomainID)
		{
			if (invKii.ContainsKey(subdomainID))
			{
				invKii[subdomainID].Dispose();
			}
			var factorization = CholeskySuiteSparse.Factorize(Kii[subdomainID], true);
			lock (invKii) invKii[subdomainID] = factorization;
			lock (Kii) Kii[subdomainID] = null; // This memory is not overwritten, but it is not needed anymore either.
		}

		public Vector MultiplyInverseKii(int subdomainID, Vector vector) => invKii[subdomainID].SolveLinearSystem(vector);

		public Vector MultiplyKbb(int subdomainID, Vector vector) => Kbb[subdomainID] * vector;

		public Vector MultiplyKbi(int subdomainID, Vector vector) => Kbi[subdomainID].Multiply(vector, false);

		public Vector MultiplyKib(int subdomainID, Vector vector) => Kbi[subdomainID].Multiply(vector, true);

		public void ReorderInternalDofs(int subdomainID)
		{
			int[] internalDofs = dofSeparator.GetDofsInternalToFree(subdomainID);
			SymmetricCscMatrix Kff = managerBasic.GetMatrixKff(subdomainID);
			(int[] rowIndicesKii, int[] colOffsetsKii) = extractors[subdomainID].ExtractSparsityPattern(Kff, internalDofs);
			bool oldToNew = false; //TODO: This should be provided by the reordering algorithm
			(int[] permutation, ReorderingStatistics stats) = reordering.FindPermutation(
				internalDofs.Length, rowIndicesKii.Length, rowIndicesKii, colOffsetsKii);

			dofSeparator.ReorderInternalDofs(subdomainID, DofPermutation.Create(permutation, oldToNew));
		}

		public class Factory : IPsmMatrixManagerFactory
		{
			public (IMatrixManager, IPsmMatrixManager) CreateMatrixManagers(IStructuralModel model, IPsmDofSeparator dofSeparator)
			{
				var basicMatrixManager = new MatrixManagerCscSymmetric(model);
				var psmMatrixManager = new PsmMatrixManagerSuiteSparse(model, dofSeparator, basicMatrixManager);
				return (basicMatrixManager, psmMatrixManager);
			}
		}
	}
}
