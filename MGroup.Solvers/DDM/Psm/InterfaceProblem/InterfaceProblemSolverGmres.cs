using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Iterative;
using ISAAR.MSolve.LinearAlgebra.Iterative.GeneralizedMinimalResidual;
using ISAAR.MSolve.LinearAlgebra.Iterative.PreconditionedConjugateGradient;
using ISAAR.MSolve.LinearAlgebra.Iterative.Preconditioning;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace MGroup.Solvers.DDM.Psm.InterfaceProblem
{
	public class InterfaceProblemSolverGmres : IInterfaceProblemSolver
	{
		public InterfaceProblemSolverGmres(GmresAlgorithm gmres)
		{
			this.GMRES = gmres;
		}

		public GmresAlgorithm GMRES { get; }

		public IterativeStatistics Solve(IInterfaceProblemMatrix matrix, IPreconditioner preconditioner, 
			Vector rhs, Vector solution, bool initialGuessIsZero)
		{
			return GMRES.Solve(matrix, preconditioner, rhs, solution, initialGuessIsZero, 
				() => Vector.CreateZero(matrix.NumRows));
		}
	}
}
