using System.Collections.Concurrent;
using System.Collections.Generic;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Triangulation;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.DomainDecomposition.Commons;
using MGroup.Solvers.DomainDecomposition.LinearAlgebraExtensions;
using MGroup.Solvers.DomainDecomposition.PSM.Dofs;
using MGroup.Solvers.DomainDecomposition.StiffnessMatrices;

namespace MGroup.Solvers.DomainDecomposition.PSM.StiffnessMatrices
{
	public class PsmMatrixManagerCSparse : IPsmMatrixManager
	{
		private readonly IPsmDofSeparator dofSeparator;
		private readonly MatrixManagerCsr managerBasic;

		private readonly Dictionary<int, SubmatrixExtractorCsrCsc> extractors = new Dictionary<int, SubmatrixExtractorCsrCsc>();
		private ConcurrentDictionary<int, CsrMatrix> Kbb = new ConcurrentDictionary<int, CsrMatrix>();
		private ConcurrentDictionary<int, CsrMatrix> Kbi = new ConcurrentDictionary<int, CsrMatrix>();
		private ConcurrentDictionary<int, CsrMatrix> Kib = new ConcurrentDictionary<int, CsrMatrix>();
		private ConcurrentDictionary<int, CscMatrix> Kii = new ConcurrentDictionary<int, CscMatrix>();
		private ConcurrentDictionary<int, LUCSparseNet> invKii = new ConcurrentDictionary<int, LUCSparseNet>();

		public PsmMatrixManagerCSparse(IStructuralModel model, IPsmDofSeparator dofSeparator,
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
			Kbb[subdomainID] = null;
			Kbi[subdomainID] = null;
			Kib[subdomainID] = null;
			Kii[subdomainID] = null;
			invKii[subdomainID] = null;
		}

		//TODO: Optimize this method. It is too slow.
		public void ExtractKiiKbbKib(int subdomainID)
		{
			int[] boundaryDofs = dofSeparator.GetSubdomainDofsBoundaryToFree(subdomainID);
			int[] internalDofs = dofSeparator.GetSubdomainDofsInternalToFree(subdomainID);

			CsrMatrix Kff = managerBasic.GetMatrixKff(subdomainID);
			extractors[subdomainID].ExtractSubmatrices(Kff, boundaryDofs, internalDofs);
			Kbb[subdomainID] = extractors[subdomainID].Submatrix00;
			Kbi[subdomainID] = extractors[subdomainID].Submatrix01;
			Kib[subdomainID] = extractors[subdomainID].Submatrix10;
			Kii[subdomainID] = extractors[subdomainID].Submatrix11;
		}

		public void InvertKii(int subdomainID)
		{
			var factorization = LUCSparseNet.Factorize(Kii[subdomainID]);
			invKii[subdomainID] = factorization;
			Kii[subdomainID] = null; // This memory is not overwritten, but it is not needed anymore either.
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
				var basicMatrixManager = new MatrixManagerCsr(model, false);
				var psmMatrixManager = new PsmMatrixManagerCSparse(model, dofSeparator, basicMatrixManager);
				return (basicMatrixManager, psmMatrixManager);
			}
		}
	}
}
