using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace MGroup.Solvers.Distributed.LinearAlgebra.IterativeMethods
{
    internal static class ExactResidual
    {
        internal static DistributedOverlappingVector Calculate(DistributedOverlappingMatrix matrix, 
            DistributedOverlappingVector rhs, DistributedOverlappingVector solution)
        {
            DistributedOverlappingVector residual = rhs.CreateZeroVectorWithSameFormat();
            Calculate(matrix, rhs, solution, residual);
            return residual;
        }

        internal static void Calculate(DistributedOverlappingMatrix matrix, DistributedOverlappingVector rhs,
            DistributedOverlappingVector solution, DistributedOverlappingVector residual)
        {
            //TODO: There is a BLAS operation y = y + a * A*x, that would be perfect for here. rhs.Copy() and then that.
            matrix.Multiply(solution, residual);
            residual.LinearCombinationIntoThis(-1.0, rhs, 1.0);
        }

    }
}