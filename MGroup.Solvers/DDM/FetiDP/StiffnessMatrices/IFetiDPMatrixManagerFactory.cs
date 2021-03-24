using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.DDM.FetiDP.Dofs;
using MGroup.Solvers.DDM.StiffnessMatrices;

namespace MGroup.Solvers.DDM.FetiDP.StiffnessMatrices
{
	public interface IFetiDPMatrixManagerFactory
	{
		(IMatrixManager, IFetiDPMatrixManager) CreateMatrixManagers(IStructuralModel model, IFetiDPDofSeparator dofSeparator);
	}
}
