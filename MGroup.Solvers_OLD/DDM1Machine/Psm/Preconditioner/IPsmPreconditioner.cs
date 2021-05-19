using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Iterative.Preconditioning;
using MGroup.Solvers_OLD.DDM.Psm.InterfaceProblem;

namespace MGroup.Solvers_OLD.DDM.Psm.Preconditioner
{
	public interface IPsmPreconditioner : IPreconditioner
	{
		void Calculate(IInterfaceProblemMatrix interfaceProblemMatrix);
	}
}
