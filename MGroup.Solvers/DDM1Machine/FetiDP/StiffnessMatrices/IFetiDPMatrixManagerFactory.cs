using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers_OLD.DDM.FetiDP.Dofs;
using MGroup.Solvers_OLD.DDM.StiffnessMatrices;

namespace MGroup.Solvers_OLD.DDM.FetiDP.StiffnessMatrices
{
	public interface IFetiDPMatrixManagerFactory
	{
		(IMatrixManager, IFetiDPMatrixManager) CreateMatrixManagers(IStructuralModel model, IFetiDPDofSeparator dofSeparator);
	}
}
