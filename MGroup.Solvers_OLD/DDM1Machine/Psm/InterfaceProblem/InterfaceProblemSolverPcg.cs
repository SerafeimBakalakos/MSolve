using System;
using System.Collections.Generic;
using System.Diagnostics;
using ISAAR.MSolve.LinearAlgebra.Iterative;
using ISAAR.MSolve.LinearAlgebra.Iterative.PreconditionedConjugateGradient;
using ISAAR.MSolve.LinearAlgebra.Iterative.Preconditioning;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers_OLD.DDM.Psm.Preconditioner;

namespace MGroup.Solvers_OLD.DDM.Psm.InterfaceProblem
{
	public class InterfaceProblemSolverPcg : IInterfaceProblemSolver
	{
		public InterfaceProblemSolverPcg(PcgAlgorithm pcg)
		{
			this.Pcg = pcg;
		}

		public PcgAlgorithm Pcg { get; }

		public IterativeStatistics Solve(IInterfaceProblemMatrix matrix, IPreconditioner preconditioner, 
			Vector rhs, Vector solution, bool initialGuessIsZero)
		{
			Pcg.Clear();
			return Pcg.Solve(matrix, preconditioner, rhs, solution, initialGuessIsZero, () => Vector.CreateZero(matrix.NumRows));
		}
	}
}
