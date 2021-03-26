using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Triangulation;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.DDM.Psm.Dofs;
using MGroup.Solvers.DDM.StiffnessMatrices;
using MGroup.Solvers.DofOrdering.Reordering;
using MGroup.Solvers.LinearAlgebraExtensions;

namespace MGroup.Solvers.DDM.Psm.StiffnessMatrices
{
	public class PsmMatrixManagerCSparseNonSymmetric : IPsmMatrixManager
	{
		private readonly IPsmDofSeparator dofSeparator;
		private readonly MatrixManagerCsr managerBasic;

		private readonly Dictionary<int, SubmatrixExtractorCsrCsc> extractors = new Dictionary<int, SubmatrixExtractorCsrCsc>();
		private Dictionary<int, CsrMatrix> Kbb = new Dictionary<int, CsrMatrix>();
		private Dictionary<int, CsrMatrix> Kbi = new Dictionary<int, CsrMatrix>();
		private Dictionary<int, CsrMatrix> Kib = new Dictionary<int, CsrMatrix>();
		private Dictionary<int, CscMatrix> Kii = new Dictionary<int, CscMatrix>();
		private Dictionary<int, LUCSparseNet> invKii = new Dictionary<int, LUCSparseNet>();

		public PsmMatrixManagerCSparseNonSymmetric(IStructuralModel model, IPsmDofSeparator dofSeparator,
			MatrixManagerCsr managerBasic)
		{
			this.dofSeparator = dofSeparator;
			this.managerBasic = managerBasic;
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				extractors[subdomain.ID] = new SubmatrixExtractorCsrCsc();
			}
		}

		public void ClearSubMatrices(int subdomainID)
		{
			lock (Kbb) Kbb[subdomainID] = null;
			lock (Kbi) Kbi[subdomainID] = null;
			lock (Kib) Kib[subdomainID] = null;
			lock (Kii) Kii[subdomainID] = null;
			lock (invKii) invKii[subdomainID] = null;
		}

		//TODO: Optimize this method. It is too slow.
		public void ExtractKiiKbbKib(int subdomainID)
		{
			int[] boundaryDofs = dofSeparator.GetSubdomainDofsBoundaryToFree(subdomainID);
			int[] internalDofs = dofSeparator.GetSubdomainDofsInternalToFree(subdomainID);

			CsrMatrix Kff = managerBasic.GetMatrixKff(subdomainID);
			extractors[subdomainID].ExtractSubmatrices(Kff, boundaryDofs, internalDofs);
			lock (Kbb) Kbb[subdomainID] = extractors[subdomainID].Submatrix00;
			lock (Kbi) Kbi[subdomainID] = extractors[subdomainID].Submatrix01;
			lock (Kib) Kib[subdomainID] = extractors[subdomainID].Submatrix10;
			lock (Kii) Kii[subdomainID] = extractors[subdomainID].Submatrix11;
		}

		public void InvertKii(int subdomainID)
		{
			var factorization = LUCSparseNet.Factorize(Kii[subdomainID]);
			lock (invKii) invKii[subdomainID] = factorization;
			lock (Kii) Kii[subdomainID] = null; // This memory is not overwritten, but it is not needed anymore either.
		}

		public Vector MultiplyInverseKii(int subdomainID, Vector vector) => invKii[subdomainID].SolveLinearSystem(vector);

		public Vector MultiplyKbb(int subdomainID, Vector vector) => Kbb[subdomainID] * vector;

		public Vector MultiplyKbi(int subdomainID, Vector vector) => Kbi[subdomainID].Multiply(vector, false);

		public Vector MultiplyKib(int subdomainID, Vector vector) => Kib[subdomainID].Multiply(vector, false);

		public void ReorderInternalDofs(int subdomainID)
		{
			dofSeparator.ReorderSubdomainInternalDofs(subdomainID, DofPermutation.CreateNoPermutation());
		}

		public class Factory : IPsmMatrixManagerFactory
		{
			public (IMatrixManager, IPsmMatrixManager) CreateMatrixManagers(IStructuralModel model, IPsmDofSeparator dofSeparator)
			{
				var basicMatrixManager = new MatrixManagerCsr(model);
				var psmMatrixManager = new PsmMatrixManagerCSparseNonSymmetric(model, dofSeparator, basicMatrixManager);
				return (basicMatrixManager, psmMatrixManager);
			}
		}
	}
}
