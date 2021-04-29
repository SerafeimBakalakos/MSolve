using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace MGroup.Solvers.Distributed.LinearAlgebra.IterativeMethods
{
    internal static class ExactResidual
    {
        internal static IIterativeMethodVector Calculate(IIterativeMethodMatrix matrix,
            IIterativeMethodVector rhs, IIterativeMethodVector solution)
        {
            IIterativeMethodVector residual = rhs.CreateZeroVectorWithSameFormat();
            Calculate(matrix, rhs, solution, residual);
            return residual;
        }

        internal static void Calculate(IIterativeMethodMatrix matrix, IIterativeMethodVector rhs,
            IIterativeMethodVector solution, IIterativeMethodVector residual)
        {
            //TODO: There is a BLAS operation y = y + a * A*x, that would be perfect for here. rhs.Copy() and then that.
            matrix.MultiplyIntoResult(solution, residual);
            residual.LinearCombinationIntoThis(-1.0, rhs, 1.0);
        }

    }
}