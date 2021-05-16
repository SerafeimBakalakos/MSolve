using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Reordering;
using ISAAR.MSolve.LinearAlgebra.Triangulation;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.DDM.Psm.Dofs;
using MGroup.Solvers.DDM.StiffnessMatrices;
using MGroup.Solvers.DofOrdering.Reordering;
using MGroup.Solvers.LinearAlgebraExtensions;
using System.Collections.Concurrent;

namespace MGroup.Solvers.DDM.Psm.StiffnessMatrices
{
	public class PsmMatrixManagerSymmetricCSparse : IPsmMatrixManager
	{
		private readonly IPsmDofSeparator dofSeparator;
		private readonly MatrixManagerCscSymmetric managerBasic;
		private readonly OrderingAmdCSparseNet reordering = new OrderingAmdCSparseNet();

		private readonly Dictionary<int, SubmatrixExtractorCsrCscSym> extractors = new Dictionary<int, SubmatrixExtractorCsrCscSym>();
		private ConcurrentDictionary<int, CsrMatrix> Kbb = new ConcurrentDictionary<int, CsrMatrix>();
		private ConcurrentDictionary<int, CsrMatrix> Kbi = new ConcurrentDictionary<int, CsrMatrix>();
		private ConcurrentDictionary<int, SymmetricCscMatrix> Kii = new ConcurrentDictionary<int, SymmetricCscMatrix>();
		private ConcurrentDictionary<int, CholeskyCSparseNet> invKii = new ConcurrentDictionary<int, CholeskyCSparseNet>();

		public PsmMatrixManagerSymmetricCSparse(IStructuralModel model, IPsmDofSeparator dofSeparator, MatrixManagerCscSymmetric managerBasic)
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
			Kbb[subdomainID] = null;
			Kbi[subdomainID] = null;
			Kii[subdomainID] = null;
			invKii[subdomainID] = null;
		}

		//TODO: Optimize this method. It is too slow.
		public void ExtractKiiKbbKib(int subdomainID)
		{
			int[] boundaryDofs = dofSeparator.GetSubdomainDofsBoundaryToFree(subdomainID);
			int[] internalDofs = dofSeparator.GetSubdomainDofsInternalToFree(subdomainID);

			SymmetricCscMatrix Kff = managerBasic.GetMatrixKff(subdomainID);
			extractors[subdomainID].ExtractSubmatrices(Kff, boundaryDofs, internalDofs);
			Kbb[subdomainID] = extractors[subdomainID].Submatrix00;
			Kbi[subdomainID] = extractors[subdomainID].Submatrix01;
			Kii[subdomainID] = extractors[subdomainID].Submatrix11;
		}

		public void InvertKii(int subdomainID)
		{
			var factorization = CholeskyCSparseNet.Factorize(Kii[subdomainID]);
			invKii[subdomainID] = factorization;
			Kii[subdomainID] = null; // This memory is not overwritten, but it is not needed anymore either.
		}

		public Vector MultiplyInverseKii(int subdomainID, Vector vector) => invKii[subdomainID].SolveLinearSystem(vector);

		public Vector MultiplyKbb(int subdomainID, Vector vector) => Kbb[subdomainID] * vector;

		public Vector MultiplyKbi(int subdomainID, Vector vector) => Kbi[subdomainID].Multiply(vector, false);

		public Vector MultiplyKib(int subdomainID, Vector vector) => Kbi[subdomainID].Multiply(vector, true);

		public void ReorderInternalDofs(int subdomainID)
		{
			int[] internalDofs = dofSeparator.GetSubdomainDofsInternalToFree(subdomainID);
			SymmetricCscMatrix Kff = managerBasic.GetMatrixKff(subdomainID);
			(int[] rowIndicesKii, int[] colOffsetsKii) = extractors[subdomainID].ExtractSparsityPattern(Kff, internalDofs);
			(int[] permutation, bool oldToNew) = reordering.FindPermutation(
				internalDofs.Length, rowIndicesKii, colOffsetsKii);

			dofSeparator.ReorderSubdomainInternalDofs(subdomainID, DofPermutation.Create(permutation, oldToNew));
		}

		public class Factory : IPsmMatrixManagerFactory
		{
			public (IMatrixManager, IPsmMatrixManager) CreateMatrixManagers(IStructuralModel model, IPsmDofSeparator dofSeparator)
			{
				var basicMatrixManager = new MatrixManagerCscSymmetric(model);
				var psmMatrixManager = new PsmMatrixManagerSymmetricCSparse(model, dofSeparator, basicMatrixManager);
				return (basicMatrixManager, psmMatrixManager);
			}
		}
	}
}
