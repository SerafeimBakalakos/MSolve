using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers_OLD.DDM.Psm.Dofs;
using MGroup.Solvers_OLD.DDM.StiffnessMatrices;

namespace MGroup.Solvers_OLD.DDM.Psm.StiffnessMatrices
{
	public interface IPsmMatrixManagerFactory
	{
		(IMatrixManager, IPsmMatrixManager) CreateMatrixManagers(IStructuralModel model, IPsmDofSeparator dofSeparator);
	}
}
