using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.DomainDecomposition.FetiDP.Dofs;
using MGroup.Solvers.DomainDecomposition.Commons;
using MGroup.Solvers.DomainDecomposition.StiffnessMatrices;

namespace MGroup.Solvers.DomainDecomposition.FetiDP.StiffnessMatrices
{
	public class FetiDPSubdomainMatrixManagerDense : IFetiDPSubdomainMatrixManager
	{
		private readonly FetiDPSubdomainDofs subdomainDofs;
		private readonly SubdomainMatrixManagerDense managerBasic;

		private Matrix Kcc;
		private Matrix KccStar;
		private Matrix Kcr;
		private Matrix Krc;
		private Matrix Krr;
		private Matrix inverseKrr;

		public FetiDPSubdomainMatrixManagerDense(FetiDPSubdomainDofs subdomainDofs, SubdomainMatrixManagerDense managerBasic)
		{
			this.subdomainDofs = subdomainDofs;
			this.managerBasic = managerBasic;
		}

		public IMatrix SchurComplementOfRemainderDofs => KccStar;

		public void CalcSchurComplementOfRemainderDofs()
		{
			KccStar = Kcc - (Kcr * (inverseKrr * Krc));
		}

		public void ClearSubMatrices()
		{
			inverseKrr = null;
			Kcc = null;
			Kcr = null;
			Krc = null;
			Krr = null;
			KccStar = null;
		}

		public void ExtractKrrKccKrc()
		{
			int[] cornerToFree = subdomainDofs.DofsCornerToFree;
			int[] remainderToFree = subdomainDofs.DofsRemainderToFree;
			Matrix Kff = managerBasic.MatrixKff;
			Kcc = Kff.GetSubmatrix(cornerToFree, cornerToFree);
			Kcr = Kff.GetSubmatrix(cornerToFree, remainderToFree);
			Krc = Kff.GetSubmatrix(remainderToFree, cornerToFree);
			Krr = Kff.GetSubmatrix(remainderToFree, remainderToFree);
		}

		public void HandleDofsWereModified() => ClearSubMatrices();

		public void InvertKrr()
		{
			inverseKrr = Krr.Invert(); ;
			Krr = null;
		}

		public Vector MultiplyInverseKrrTimes(Vector vector) => inverseKrr * vector;

		public Vector MultiplyKccTimes(Vector vector) => Kcc * vector;

		public Vector MultiplyKcrTimes(Vector vector) => Kcr * vector;

		public Vector MultiplyKrcTimes(Vector vector) => Krc * vector;

		public void ReorderRemainderDofs() => subdomainDofs.ReorderRemainderDofs(DofPermutation.CreateNoPermutation());

		public class Factory : IFetiDPSubdomainMatrixManagerFactory
		{
			public (ISubdomainMatrixManager, IFetiDPSubdomainMatrixManager) CreateMatrixManagers(
				ISubdomain subdomain, FetiDPSubdomainDofs subdomainDofs)
			{
				var basicMatrixManager = new SubdomainMatrixManagerDense(subdomain);
				var fetiDPMatrixManager = new FetiDPSubdomainMatrixManagerDense(subdomainDofs, basicMatrixManager);
				return (basicMatrixManager, fetiDPMatrixManager);
			}
		}
	}
}
