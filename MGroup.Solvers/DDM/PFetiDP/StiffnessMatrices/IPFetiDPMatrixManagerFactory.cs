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
	public interface IPFetiDPMatrixManagerFactory
	{
		(IMatrixManager, IPsmMatrixManager, IFetiDPMatrixManager) CreateMatrixManagers(
			IStructuralModel model, IPsmDofSeparator dofSeparatorPsm, IFetiDPDofSeparator dofSeparatorFetiDP);
	}
}
