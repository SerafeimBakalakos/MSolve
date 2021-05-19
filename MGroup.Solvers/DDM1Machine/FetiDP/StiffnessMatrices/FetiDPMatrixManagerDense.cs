using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Solvers.LinearSystems;
using MGroup.Solvers_OLD.Assemblers;
using MGroup.Solvers_OLD.DDM.FetiDP.Dofs;
using MGroup.Solvers_OLD.DDM.StiffnessMatrices;
using MGroup.Solvers_OLD.DofOrdering.Reordering;
using MGroup.Solvers_OLD.LinearSystems;

namespace MGroup.Solvers_OLD.DDM.FetiDP.StiffnessMatrices
{
	public class FetiDPMatrixManagerDense : IFetiDPMatrixManager
	{
		private readonly IFetiDPDofSeparator dofSeparator;
		private readonly MatrixManagerDense managerBasic;

		private Dictionary<int, Matrix> Kcc = new Dictionary<int, Matrix>();
		private Dictionary<int, Matrix> KccStar = new Dictionary<int, Matrix>();
		private Dictionary<int, Matrix> Kcr = new Dictionary<int, Matrix>();
		private Dictionary<int, Matrix> Krc = new Dictionary<int, Matrix>();
		private Dictionary<int, Matrix> Krr = new Dictionary<int, Matrix>();
		private Dictionary<int, Matrix> inverseKrr = new Dictionary<int, Matrix>();

		public FetiDPMatrixManagerDense(IFetiDPDofSeparator dofSeparator, MatrixManagerDense managerBasic)
		{
			this.dofSeparator = dofSeparator;
			this.managerBasic = managerBasic;
		}

		public IMatrix GetSchurComplementOfRemainderDofs(int subdomainID) => KccStar[subdomainID];

		public void CalcSchurComplementOfRemainderDofs(int subdomainID)
		{
			Matrix kccStar = Kcc[subdomainID] - (Kcr[subdomainID] * (inverseKrr[subdomainID] * Krc[subdomainID]));
			lock (KccStar) KccStar[subdomainID] = kccStar;
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
			Matrix Kff = managerBasic.GetMatrixKff(subdomainID);
			lock (Kcc) Kcc[subdomainID] = Kff.GetSubmatrix(cornerToFree, cornerToFree);
			lock (Kcr) Kcr[subdomainID] = Kff.GetSubmatrix(cornerToFree, remainderToFree);
			lock (Krc) Krc[subdomainID] = Kff.GetSubmatrix(remainderToFree, cornerToFree);
			lock (Krr) Krr[subdomainID] = Kff.GetSubmatrix(remainderToFree, remainderToFree);
		}

		public void InvertKrr(int subdomainID)
		{
			Matrix inverse = Krr[subdomainID].Invert();
			lock (inverseKrr) inverseKrr[subdomainID] = inverse;
			lock (Krr) Krr[subdomainID] = null;
		}

		public Vector MultiplyInverseKrrTimes(int subdomainID, Vector vector) => inverseKrr[subdomainID] * vector;

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
				var basicMatrixManager = new MatrixManagerDense(model);
				var fetiDPMatrixManager = new FetiDPMatrixManagerDense(dofSeparator, basicMatrixManager);
				return (basicMatrixManager, fetiDPMatrixManager);
			}
		}
	}
}
