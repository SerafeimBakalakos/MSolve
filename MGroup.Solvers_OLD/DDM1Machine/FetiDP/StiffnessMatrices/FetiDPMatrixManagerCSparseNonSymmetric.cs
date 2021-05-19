using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Triangulation;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers_OLD.DDM.FetiDP.Dofs;
using MGroup.Solvers_OLD.DDM.StiffnessMatrices;
using MGroup.Solvers_OLD.DofOrdering.Reordering;
using MGroup.Solvers_OLD.LinearAlgebraExtensions;

namespace MGroup.Solvers_OLD.DDM.FetiDP.StiffnessMatrices
{
	public class FetiDPMatrixManagerCSparseNonSymmetric : IFetiDPMatrixManager
	{
		private readonly IFetiDPDofSeparator dofSeparator;
		private readonly MatrixManagerCsr managerBasic;
		
		private readonly Dictionary<int, SubmatrixExtractorFullCsrCsc> extractors = new Dictionary<int, SubmatrixExtractorFullCsrCsc>();
		private Dictionary<int, Matrix> Kcc = new Dictionary<int, Matrix>();
		private Dictionary<int, Matrix> KccStar = new Dictionary<int, Matrix>();
		private Dictionary<int, CsrMatrix> Kcr = new Dictionary<int, CsrMatrix>();
		private Dictionary<int, CsrMatrix> Krc = new Dictionary<int, CsrMatrix>();
		private Dictionary<int, CscMatrix> Krr = new Dictionary<int, CscMatrix>();
		private Dictionary<int, LUCSparseNet> inverseKrr = new Dictionary<int, LUCSparseNet>();

		public FetiDPMatrixManagerCSparseNonSymmetric(
			IStructuralModel model, IFetiDPDofSeparator dofSeparator, MatrixManagerCsr managerBasic)
		{
			this.dofSeparator = dofSeparator;
			this.managerBasic = managerBasic;
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				extractors[subdomain.ID] = new SubmatrixExtractorFullCsrCsc();
			}
		}

		public IMatrix GetSchurComplementOfRemainderDofs(int subdomainID) => KccStar[subdomainID];

		public void CalcSchurComplementOfRemainderDofs(int subdomainID)
		{
			int s = subdomainID;
			var kccStar = Matrix.CreateZero(Kcc[s].NumRows, Kcc[s].NumColumns);
			SchurComplementFullCsrCsc.CalcSchurComplement(Kcc[s], Kcr[s], Krc[s], inverseKrr[s], kccStar);
			lock (KccStar) KccStar[s] = kccStar;
		}

		public void ClearSubMatrices(int subdomainID)
		{
			lock (inverseKrr) inverseKrr[subdomainID] = null;
			lock (Kcc) Kcc[subdomainID] = null;
			lock (Kcr) Kcr[subdomainID] = null;
			lock (Krc) Krc[subdomainID] = null;
			lock (Krr) Krr[subdomainID] = null;
			lock (KccStar) KccStar[subdomainID] = null;
		}

		public void ExtractKrrKccKrc(int subdomainID)
		{
			int[] cornerToFree = dofSeparator.GetDofsCornerToFree(subdomainID);
			int[] remainderToFree = dofSeparator.GetDofsRemainderToFree(subdomainID);

			CsrMatrix Kff = managerBasic.GetMatrixKff(subdomainID);
			extractors[subdomainID].ExtractSubmatrices(Kff, cornerToFree, remainderToFree);
			lock (Kcc) Kcc[subdomainID] = extractors[subdomainID].Submatrix00;
			lock (Kcr) Kcr[subdomainID] = extractors[subdomainID].Submatrix01;
			lock (Krc) Krc[subdomainID] = extractors[subdomainID].Submatrix10;
			lock (Krr) Krr[subdomainID] = extractors[subdomainID].Submatrix11;
		}

		public void InvertKrr(int subdomainID)
		{
			var factorization = LUCSparseNet.Factorize(Krr[subdomainID]);
			lock (inverseKrr) inverseKrr[subdomainID] = factorization;
			lock (Krr) Krr[subdomainID] = null;
		}

		public Vector MultiplyInverseKrrTimes(int subdomainID, Vector vector) 
			=> inverseKrr[subdomainID].SolveLinearSystem(vector);

		public Vector MultiplyKccTimes(int subdomainID, Vector vector) => Kcc[subdomainID] * vector;

		public Vector MultiplyKcrTimes(int subdomainID, Vector vector) => Kcr[subdomainID] * vector;

		public Vector MultiplyKrcTimes(int subdomainID, Vector vector) => Krc[subdomainID] * vector;

		public void ReorderRemainderDofs(int subdomainID)
		{
			dofSeparator.ReorderRemainderDofs(subdomainID, DofPermutation.CreateNoPermutation());
		}

		public class Factory : IFetiDPMatrixManagerFactory
		{
			public (IMatrixManager, IFetiDPMatrixManager) CreateMatrixManagers(IStructuralModel model, IFetiDPDofSeparator dofSeparator)
			{
				var basicMatrixManager = new MatrixManagerCsr(model);
				var fetiDPMatrixManager = new FetiDPMatrixManagerCSparseNonSymmetric(model, dofSeparator, basicMatrixManager);
				return (basicMatrixManager, fetiDPMatrixManager);
			}
		}
	}
}
