using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Reordering;
using ISAAR.MSolve.LinearAlgebra.Triangulation;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.DomainDecomposition.FetiDP.Dofs;
using MGroup.Solvers.DomainDecomposition.StiffnessMatrices;
using MGroup.Solvers.DomainDecomposition.LinearAlgebraExtensions;
using MGroup.Solvers.DomainDecomposition.Commons;

namespace MGroup.Solvers.DomainDecomposition.FetiDP.StiffnessMatrices
{
	public class FetiDPSubdomainMatrixManagerSymmetricCSparse : IFetiDPSubdomainMatrixManager
	{
		private readonly FetiDPSubdomainDofs subdomainDofs;
		private readonly SubdomainMatrixManagerSymmetricCsc managerBasic;
		private readonly OrderingAmdCSparseNet reordering = new OrderingAmdCSparseNet();
		private readonly SubmatrixExtractorPckCsrCscSym submatrixExtractor = new SubmatrixExtractorPckCsrCscSym();

		private SymmetricMatrix Kcc;
		private SymmetricMatrix KccStar;
		private CsrMatrix Kcr;
		private SymmetricCscMatrix Krr;
		private CholeskyCSparseNet inverseKrr;

		public FetiDPSubdomainMatrixManagerSymmetricCSparse(FetiDPSubdomainDofs subdomainDofs,
			SubdomainMatrixManagerSymmetricCsc managerBasic)
		{
			this.subdomainDofs = subdomainDofs;
			this.managerBasic = managerBasic;
		}

		public IMatrix SchurComplementOfRemainderDofs => KccStar;

		public void CalcSchurComplementOfRemainderDofs()
		{
			KccStar = SymmetricMatrix.CreateZero(Kcc.Order);
			SchurComplementPckCsrCscSym.CalcSchurComplement(Kcc, Kcr, inverseKrr, KccStar);
		}

		public void ClearSubMatrices()
		{
			inverseKrr = null;
			Kcc = null;
			Kcr = null;
			Krr = null;
			KccStar = null;
		}

		public void ExtractKrrKccKrc()
		{
			int[] cornerToFree = subdomainDofs.DofsCornerToFree;
			int[] remainderToFree = subdomainDofs.DofsRemainderToFree;

			SymmetricCscMatrix Kff = managerBasic.MatrixKff;
			submatrixExtractor.ExtractSubmatrices(Kff, cornerToFree, remainderToFree);
			Kcc = submatrixExtractor.Submatrix00;
			Kcr = submatrixExtractor.Submatrix01;
			Krr = submatrixExtractor.Submatrix11; 

			//TODO: It would be better if these were returned by the extractor, instead of stored in its properties. 
			//		The only state that the extractor needs is its private mapping arrays
		}

		public void HandleDofsWereModified()
		{
			ClearSubMatrices();
			submatrixExtractor.Clear();
		}

		public void InvertKrr()
		{
			var factorization = CholeskyCSparseNet.Factorize(Krr);
			inverseKrr = factorization;
			Krr = null; // It has not been mutated, but it is no longer needed
		}

		public Vector MultiplyInverseKrrTimes(Vector vector) => inverseKrr.SolveLinearSystem(vector);

		public Vector MultiplyKccTimes(Vector vector) => Kcc * vector;

		public Vector MultiplyKcrTimes(Vector vector) => Kcr * vector;

		public Vector MultiplyKrcTimes(Vector vector) => Kcr.Multiply(vector, true);

		public void ReorderRemainderDofs()
		{
			int[] remainderDofs = subdomainDofs.DofsRemainderToFree;
			SymmetricCscMatrix Kff = managerBasic.MatrixKff;
			(int[] rowIndicesKrr, int[] colOffsetsKrr) = submatrixExtractor.ExtractSparsityPattern(Kff, remainderDofs);
			(int[] permutation, bool oldToNew) = reordering.FindPermutation(
				remainderDofs.Length, rowIndicesKrr, colOffsetsKrr);

			subdomainDofs.ReorderRemainderDofs(DofPermutation.Create(permutation, oldToNew));
		}

		public class Factory : IFetiDPSubdomainMatrixManagerFactory
		{
			public (ISubdomainMatrixManager, IFetiDPSubdomainMatrixManager) CreateMatrixManagers(
				ISubdomain subdomain, FetiDPSubdomainDofs subdomainDofs)
			{
				var basicMatrixManager = new SubdomainMatrixManagerSymmetricCsc(subdomain);
				var fetiDPMatrixManager = new FetiDPSubdomainMatrixManagerSymmetricCSparse(subdomainDofs, basicMatrixManager);
				return (basicMatrixManager, fetiDPMatrixManager);
			}
		}
	}
}
