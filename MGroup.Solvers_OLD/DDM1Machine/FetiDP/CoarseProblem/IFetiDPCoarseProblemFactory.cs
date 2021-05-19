using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers_OLD.DDM.Environments;

namespace MGroup.Solvers_OLD.DDM.FetiDP.CoarseProblem
{
	public interface IFetiDPCoarseProblemFactory
	{
		IFetiDPCoarseProblem Create(IDdmEnvironment environment, IStructuralModel model);
	}
}
