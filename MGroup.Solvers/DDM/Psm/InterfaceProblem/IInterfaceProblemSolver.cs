using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Iterative;
using ISAAR.MSolve.LinearAlgebra.Iterative.Preconditioning;
using ISAAR.MSolve.LinearAlgebra.Vectors;

//TODO: How to calculate convergence?
namespace MGroup.Solvers.DDM.Psm.InterfaceProblem
{
	public interface IInterfaceProblemSolver
	{
		IterativeStatistics Solve(IInterfaceProblemMatrix matrix, IPreconditioner preconditioner, Vector rhs, Vector solution,
			bool initialGuessIsZero);
	}
}
