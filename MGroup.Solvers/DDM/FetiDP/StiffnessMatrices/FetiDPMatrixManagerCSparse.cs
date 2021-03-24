using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Reordering;
using ISAAR.MSolve.LinearAlgebra.Triangulation;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.DDM.FetiDP.Dofs;
using MGroup.Solvers.DDM.StiffnessMatrices;
using MGroup.Solvers.DofOrdering.Reordering;
using MGroup.Solvers.LinearAlgebraExtensions;

namespace MGroup.Solvers.DDM.FetiDP.StiffnessMatrices
{
	public class FetiDPMatrixManagerCSparse : IFetiDPMatrixManager
	{
		private readonly IFetiDPDofSeparator dofSeparator;
		private readonly MatrixManagerCscSymmetric managerBasic;
		private readonly OrderingAmdCSparseNet reordering = new OrderingAmdCSparseNet();

		private readonly Dictionary<int, SubmatrixExtractorPckCsrCscSym> extractors = new Dictionary<int, SubmatrixExtractorPckCsrCscSym>();
		private Dictionary<int, SymmetricMatrix> Kcc = new Dictionary<int, SymmetricMatrix>();
		private Dictionary<int, SymmetricMatrix> KccStar = new Dictionary<int, SymmetricMatrix>();
		private Dictionary<int, CsrMatrix> Kcr = new Dictionary<int, CsrMatrix>();
		private Dictionary<int, SymmetricCscMatrix> Krr = new Dictionary<int, SymmetricCscMatrix>();
		private Dictionary<int, CholeskyCSparseNet> inverseKrr = new Dictionary<int, CholeskyCSparseNet>();

		public FetiDPMatrixManagerCSparse(IStructuralModel model, IFetiDPDofSeparator dofSeparator, 
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
			lock (KccStar) KccStar[s] = kccStar;
		}

		public void ClearSubMatrices(int subdomainID)
		{
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
			var factorization = CholeskyCSparseNet.Factorize(Krr[subdomainID]);
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
			(int[] permutation, bool oldToNew) = reordering.FindPermutation(
				remainderDofs.Length, rowIndicesKrr, colOffsetsKrr);

			dofSeparator.ReorderRemainderDofs(subdomainID, DofPermutation.Create(permutation, oldToNew));
		}

		public class Factory : IFetiDPMatrixManagerFactory
		{
			public (IMatrixManager, IFetiDPMatrixManager) CreateMatrixManagers(IStructuralModel model, IFetiDPDofSeparator dofSeparator)
			{
				var basicMatrixManager = new MatrixManagerCscSymmetric(model);
				var fetiDPMatrixManager = new FetiDPMatrixManagerCSparse(model, dofSeparator, basicMatrixManager);
				return (basicMatrixManager, fetiDPMatrixManager);
			}
		}
	}
}
