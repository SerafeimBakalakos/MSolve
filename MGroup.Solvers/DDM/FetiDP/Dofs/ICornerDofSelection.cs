using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;

namespace MGroup.Solvers.DDM.FetiDP.Dofs
{
	public interface ICornerDofSelection
	{
		bool IsCornerDof(INode node, IDofType type);

		int[] CornerNodeIDs { get; }
	}
}
