using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Solvers.LinearSystems;
using MGroup.Solvers.Assemblers;
using MGroup.Solvers.DDM.Dofs;
using MGroup.Solvers.DDM.FetiDP.Dofs;
using MGroup.Solvers.DDM.FetiDP.StiffnessMatrices;
using MGroup.Solvers.DDM.Psm.Dofs;
using MGroup.Solvers.DDM.Psm.StiffnessMatrices;
using MGroup.Solvers.DDM.StiffnessMatrices;

namespace MGroup.Solvers.DDM.PFetiDP.StiffnessMatrices
{
	public class PFetiDPMatrixManagerFactorySuiteSparse : IPFetiDPMatrixManagerFactory
	{
		public (IMatrixManager, IPsmMatrixManager, IFetiDPMatrixManager) CreateMatrixManagers(
			IStructuralModel model, IPsmDofSeparator dofSeparatorPsm, IFetiDPDofSeparator dofSeparatorFetiDP)
		{
			var basicMatrixManager = new MatrixManagerCscSymmetric(model);
			var psmMatrixManager = new PsmMatrixManagerSuiteSparse(model, dofSeparatorPsm, basicMatrixManager);
			var fetiDPMatrixManager = new FetiDPMatrixManagerSuiteSparse(model, dofSeparatorFetiDP, basicMatrixManager);
			return (basicMatrixManager, psmMatrixManager, fetiDPMatrixManager);
		}
	}
}
