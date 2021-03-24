using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.DDM.FetiDP.Dofs;
using MGroup.Solvers.DDM.FetiDP.StiffnessMatrices;
using MGroup.Solvers.DDM.Psm.Dofs;
using MGroup.Solvers.DDM.Psm.StiffnessMatrices;
using MGroup.Solvers.DDM.StiffnessMatrices;

namespace MGroup.Solvers.DDM.PFetiDP.StiffnessMatrices
{
	public class PFetiDPMatrixManagerFactoryCSparseNonSymmetric : IPFetiDPMatrixManagerFactory
	{
		public (IMatrixManager, IPsmMatrixManager, IFetiDPMatrixManager) CreateMatrixManagers(
			IStructuralModel model, IPsmDofSeparator dofSeparatorPsm, IFetiDPDofSeparator dofSeparatorFetiDP)
		{
			var basicMatrixManager = new MatrixManagerCsr(model);
			var psmMatrixManager = new PsmMatrixManagerCSparseNonSymmetric(model, dofSeparatorPsm, basicMatrixManager);
			var fetiDPMatrixManager = new FetiDPMatrixManagerCSparseNonSymmetric(model, dofSeparatorFetiDP, basicMatrixManager);
			return (basicMatrixManager, psmMatrixManager, fetiDPMatrixManager);
		}
	}
}
