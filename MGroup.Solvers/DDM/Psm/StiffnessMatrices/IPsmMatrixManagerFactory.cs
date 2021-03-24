using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.DDM.Dofs;
using MGroup.Solvers.DDM.Psm.Dofs;
using MGroup.Solvers.DDM.StiffnessMatrices;

namespace MGroup.Solvers.DDM.Psm.StiffnessMatrices
{
	public interface IPsmMatrixManagerFactory
	{
		(IMatrixManager, IPsmMatrixManager) CreateMatrixManagers(IStructuralModel model, IPsmDofSeparator dofSeparator);
	}
}
