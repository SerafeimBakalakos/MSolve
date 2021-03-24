using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.DDM.Environments;

namespace MGroup.Solvers.DDM.FetiDP.CoarseProblem
{
	public interface IFetiDPCoarseProblemFactory
	{
		IFetiDPCoarseProblem Create(IProcessingEnvironment environment, IStructuralModel model);
	}
}
