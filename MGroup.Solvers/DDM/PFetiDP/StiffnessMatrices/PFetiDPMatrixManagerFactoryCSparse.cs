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
	public class PFetiDPMatrixManagerFactoryCSparse : IPFetiDPMatrixManagerFactory
	{
		public (IMatrixManager, IPsmMatrixManager, IFetiDPMatrixManager) CreateMatrixManagers(
			IStructuralModel model, IPsmDofSeparator dofSeparatorPsm, IFetiDPDofSeparator dofSeparatorFetiDP)
		{
			var basicMatrixManager = new MatrixManagerCscSymmetric(model);
			var psmMatrixManager = new PsmMatrixManagerSymmetricCSparse(model, dofSeparatorPsm, basicMatrixManager);
			var fetiDPMatrixManager = new FetiDPMatrixManagerCSparse(model, dofSeparatorFetiDP, basicMatrixManager);
			return (basicMatrixManager, psmMatrixManager, fetiDPMatrixManager);
		}
	}
}
