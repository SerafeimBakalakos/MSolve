using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Reordering;
using ISAAR.MSolve.LinearAlgebra.Triangulation;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers_OLD.DDM.FetiDP.Dofs;
using MGroup.Solvers_OLD.DDM.StiffnessMatrices;
using MGroup.Solvers_OLD.DofOrdering.Reordering;
using MGroup.Solvers_OLD.LinearAlgebraExtensions;

namespace MGroup.Solvers_OLD.DDM.FetiDP.StiffnessMatrices
{
	public class FetiDPMatrixManagerSuiteSparse : IFetiDPMatrixManager
	{
		private readonly IFetiDPDofSeparator dofSeparator;
		private readonly MatrixManagerCscSymmetric managerBasic;
		private readonly OrderingAmdSuiteSparse reordering = new OrderingAmdSuiteSparse();

		private readonly Dictionary<int, SubmatrixExtractorPckCsrCscSym> extractors = new Dictionary<int, SubmatrixExtractorPckCsrCscSym>();
		private Dictionary<int, SymmetricMatrix> Kcc = new Dictionary<int, SymmetricMatrix>();
		private Dictionary<int, SymmetricMatrix> KccStar = new Dictionary<int, SymmetricMatrix>();
		private Dictionary<int, CsrMatrix> Kcr = new Dictionary<int, CsrMatrix>();
		private Dictionary<int, SymmetricCscMatrix> Krr = new Dictionary<int, SymmetricCscMatrix>();
		private Dictionary<int, CholeskySuiteSparse> inverseKrr = new Dictionary<int, CholeskySuiteSparse>();

		public FetiDPMatrixManagerSuiteSparse(IStructuralModel model, IFetiDPDofSeparator dofSeparator, 
			MatrixManagerCscSymmetric managerBasic)
		{
			this.dofSeparator = dofSeparator;
			this.managerBasic = managerBasic;
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				extractors[subdomain.ID] = new SubmatrixExtractorPckCsrCscSym();
			}
		}

		public IMatrix GetSchurComplementOfRemainderDofs(int subdomainID) => KccStar[subdomainID];

		public void CalcSchurComplementOfRemainderDofs(int subdomainID)
		{
			int s = subdomainID;
			var kccStar = SymmetricMatrix.CreateZero(Kcc[s].Order);
			SchurComplementPckCsrCscSym.CalcSchurComplement(Kcc[s], Kcr[s], inverseKrr[s], kccStar);
			lock(KccStar) KccStar[s] = kccStar;

		}

		public void ClearSubMatrices(int subdomainID)
		{
			if (inverseKrr.ContainsKey(subdomainID))
			{
				inverseKrr[subdomainID].Dispose();
			}
			lock (inverseKrr) inverseKrr[subdomainID] = null;
			lock (Kcc) Kcc[subdomainID] = null;
			lock (Kcr) Kcr[subdomainID] = null;
			lock (Krr) Krr[subdomainID] = null;
			lock (KccStar) KccStar[subdomainID] = null;
		}

		public void ExtractKrrKccKrc(int subdomainID)
		{
			int[] cornerToFree = dofSeparator.GetDofsCornerToFree(subdomainID);
			int[] remainderToFree = dofSeparator.GetDofsRemainderToFree(subdomainID);

			SymmetricCscMatrix Kff = managerBasic.GetMatrixKff(subdomainID);
			extractors[subdomainID].ExtractSubmatrices(Kff, cornerToFree, remainderToFree);
			lock (Kcc) Kcc[subdomainID] = extractors[subdomainID].Submatrix00;
			lock (Kcr) Kcr[subdomainID] = extractors[subdomainID].Submatrix01;
			lock (Krr) Krr[subdomainID] = extractors[subdomainID].Submatrix11;
		}

		public void InvertKrr(int subdomainID)
		{
			if (inverseKrr.ContainsKey(subdomainID))
			{
				inverseKrr[subdomainID].Dispose();
			}
			var factorization = CholeskySuiteSparse.Factorize(Krr[subdomainID], true);
			lock (inverseKrr) inverseKrr[subdomainID] = factorization;
			lock (Krr) Krr[subdomainID] = null;
		}

		public Vector MultiplyInverseKrrTimes(int subdomainID, Vector vector) 
			=> inverseKrr[subdomainID].SolveLinearSystem(vector);

		public Vector MultiplyKccTimes(int subdomainID, Vector vector) => Kcc[subdomainID] * vector;

		public Vector MultiplyKcrTimes(int subdomainID, Vector vector) => Kcr[subdomainID] * vector;

		public Vector MultiplyKrcTimes(int subdomainID, Vector vector) => Kcr[subdomainID].Multiply(vector, true);

		public void ReorderRemainderDofs(int subdomainID)
		{
			int[] remainderDofs = dofSeparator.GetDofsRemainderToFree(subdomainID);
			SymmetricCscMatrix Kff = managerBasic.GetMatrixKff(subdomainID);
			(int[] rowIndicesKrr, int[] colOffsetsKrr) = extractors[subdomainID].ExtractSparsityPattern(Kff, remainderDofs);

			bool oldToNew = false; //TODO: This should be provided by the reordering algorithm
			(int[] permutation, ReorderingStatistics stats) = reordering.FindPermutation(
				remainderDofs.Length, rowIndicesKrr.Length, rowIndicesKrr, colOffsetsKrr);

			dofSeparator.ReorderRemainderDofs(subdomainID, DofPermutation.Create(permutation, oldToNew));
		}

		public class Factory : IFetiDPMatrixManagerFactory
		{
			public (IMatrixManager, IFetiDPMatrixManager) CreateMatrixManagers(IStructuralModel model, IFetiDPDofSeparator dofSeparator)
			{
				var basicMatrixManager = new MatrixManagerCscSymmetric(model);
				var fetiDPMatrixManager = new FetiDPMatrixManagerSuiteSparse(model, dofSeparator, basicMatrixManager);
				return (basicMatrixManager, fetiDPMatrixManager);
			}
		}
	}
}
