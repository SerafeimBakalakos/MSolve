using System;
using System.Diagnostics;
using ISAAR.MSolve.LinearAlgebra.Iterative.ConjugateGradient;
using ISAAR.MSolve.LinearAlgebra.Iterative.Termination;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace ISAAR.MSolve.LinearAlgebra.Iterative.PreconditionedConjugateGradient
{
    /// <summary>
    /// Implements the untransformed Preconditioned Conjugate Gradient algorithm for solving linear systems with symmetric 
    /// positive definite matrices. This implementation is based on the algorithm presented in section B3 of 
    /// "An Introduction to the Conjugate Gradient Method Without the Agonizing Pain", Jonathan Richard Shewchuk, 1994
    /// Authors: Serafeim Bakalakos
    /// </summary>
    public class PcgAlgorithmForGsi : PcgAlgorithmBase
    {
        private const string name = "Preconditioned Conjugate Gradient";
        private readonly IPcgBetaParameterCalculation betaCalculation;
        private readonly double convergenceTol;

        private PcgAlgorithmForGsi(double residualTolerance, IMaxIterationsProvider maxIterationsProvider,
            IPcgResidualUpdater residualUpdater, IPcgBetaParameterCalculation betaCalculation, double convergenceTol) : 
            base(residualTolerance, maxIterationsProvider, null, residualUpdater)
        {
            this.betaCalculation = betaCalculation;
            this.convergenceTol = convergenceTol;
        }

        protected override IterativeStatistics SolveInternal(int maxIterations, Func<IVector> zeroVectorInitializer)
        {
            iteration = 0;

            double residualNorm0 = residual.Norm2();

            // In contrast to the source algorithm, we initialize s here. At each iteration it will be overwritten, 
            // thus avoiding allocating & deallocating a new vector.
            precondResidual = zeroVectorInitializer();

            // d = inv(M) * r
            direction = zeroVectorInitializer();
            Preconditioner.SolveLinearSystem(residual, direction);

            // δnew = δ0 = r * d
            resDotPrecondRes = residual.DotProduct(direction);

            // The convergence and beta strategies must be initialized immediately after the first r and r*inv(M)*r are computed.
            betaCalculation.Initialize(this);

            // This is also used as output
            double residualNormRatio = double.NaN;

            // Allocate memory for other vectors, which will be reused during each iteration
            matrixTimesDirection = zeroVectorInitializer();

            for (iteration = 1; iteration < maxIterations; ++iteration)
            {
                // q = A * d
                Matrix.Multiply(direction, matrixTimesDirection);

                // α = δnew / (d * q)
                stepSize = resDotPrecondRes / direction.DotProduct(matrixTimesDirection);

                // x = x + α * d
                solution.AxpyIntoThis(direction, stepSize);

                // Normally the residual vector is updated as: r = r - α * q. However corrections might need to be applied.
                residualUpdater.UpdateResidual(this, residual);

                // At this point we can check if CG has converged and exit, thus avoiding the uneccesary operations that follow.
                residualNormRatio = residual.Norm2() / residualNorm0;
                Debug.WriteLine($"PCG Iteration = {iteration}: residual norm ratio = {residualNormRatio}");
                if (residualNormRatio <= convergenceTol)
                {
                    return new IterativeStatistics
                    {
                        AlgorithmName = name,
                        HasConverged = true,
                        NumIterationsRequired = iteration + 1,
                        ResidualNormRatioEstimation = residualNormRatio
                    };
                }

                // s = inv(M) * r
                Preconditioner.SolveLinearSystem(residual, precondResidual);

                // δold = δnew
                resDotPrecondResOld = resDotPrecondRes;

                // δnew = r * s 
                resDotPrecondRes = residual.DotProduct(precondResidual);

                // The default Fletcher-Reeves formula is: β = δnew / δold = (sNew * rNew) / (sOld * rOld)
                // However we could use a different one, e.g. for variable preconditioning Polak-Ribiere is usually better.
                paramBeta = betaCalculation.CalculateBeta(this);

                // d = s + β * d
                //TODO: benchmark the two options to find out which is faster
                //direction = preconditionedResidual.Axpy(direction, beta); //This allocates a new vector d, copies r and GCs the existing d.
                direction.LinearCombinationIntoThis(paramBeta, precondResidual, 1.0); //This performs additions instead of copying and needless multiplications.
            }

            // We reached the max iterations before PCG converged
            return new IterativeStatistics
            {
                AlgorithmName = name,
                HasConverged = false,
                NumIterationsRequired = maxIterations,
                ResidualNormRatioEstimation = residualNormRatio
            };
        }

        /// <summary>
        /// Constructs <see cref="PcgAlgorithm"/> instances, allows the user to specify some or all of the required parameters 
        /// and provides defaults for the rest.
        /// Author: Serafeim Bakalakos
        /// </summary>
        public class Builder : PcgBuilderBase
        {
            /// <summary>
            /// Specifies how to calculate the beta parameter of PCG, which is used to update the direction vector. 
            /// </summary>
            public IPcgBetaParameterCalculation BetaCalculation { get; set; } = new FletcherReevesBeta();

            /// <summary>
            /// Creates a new instance of <see cref="PcgAlgorithm"/>.
            /// </summary>
            public PcgAlgorithmForGsi Build()
            {
                return new PcgAlgorithmForGsi(ResidualTolerance, MaxIterationsProvider, ResidualUpdater,
                    BetaCalculation, ResidualTolerance);
            }
        }
    }
}
