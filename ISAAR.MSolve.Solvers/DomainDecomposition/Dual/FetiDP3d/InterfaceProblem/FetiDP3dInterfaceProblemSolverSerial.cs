using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Iterative;
using ISAAR.MSolve.LinearAlgebra.Iterative.PreconditionedConjugateGradient;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.FlexibilityMatrix;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.Augmentation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Pcg;
using ISAAR.MSolve.Solvers.Logging;

//TODO: This class should not exist. Instead the corresponding one in FETI-DP 2D should be used. The only difference is in the 
//      calculation of dr, fcStarTilde, which are assigned to MatrixManager component. For now only the 3D MatrixManager provides
//      these vectors, but they should be in 2D as well. This would also remove the need for casting
//TODO: IAugmentationConstraints augmentationConstraints are injected into the constructor since they do not exist in 2D FETI-DP.
//      Perhaps LagrangeEnumerator and MatrixManager should also be injected
//TODO: Reorder both corner and augmented dofs and store the coarse problem dof ordering. Do this in matrix manager. Then use it 
//      here (at least) for fcStarTilde. The current approach is to list all augmented dofs after all corner and apply permutations
//      before and after solving linear systems with the coarse problem matrix KccStarTilde.
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.InterfaceProblem
{
    /// <summary>
    /// The interface problem is solved using PCG. The matrix of the coarse problem KccStar, namely the static condensation of 
    /// the remainder dofs onto the corner dofs is performed explicitly.
    /// </summary>
    public class FetiDP3dInterfaceProblemSolverSerial : IFetiDPInterfaceProblemSolver
    {
        private readonly IAugmentationConstraints augmentationConstraints;
        private readonly IModel model;
        private readonly PcgSettings pcgSettings;

        public Vector previousLambda { get; set; }

        public bool usePreviousLambda;

        public FetiDP3dInterfaceProblemSolverSerial(IModel model, PcgSettings pcgSettings,
            IAugmentationConstraints augmentationConstraints)
        {
            this.model = model;
            this.pcgSettings = pcgSettings;
            this.augmentationConstraints = augmentationConstraints;
        }

        public Vector SolveInterfaceProblem(IFetiDPMatrixManager matrixManager,
            ILagrangeMultipliersEnumerator lagrangesEnumerator, IFetiDPFlexibilityMatrix flexibility,
            IFetiPreconditioner preconditioner, double globalForcesNorm, ISolverLogger logger)
        {
            int systemOrder = flexibility.NumGlobalLagrangeMultipliers;

            // Prepare PCG matrix, preconditioner, rhs and solution
            var pcgMatrix = new FetiDPInterfaceProblemMatrixSerial(matrixManager, flexibility);
            var pcgPreconditioner = new FetiDPInterfaceProblemPreconditioner(preconditioner);
            Vector globalDr = ((IFetiDP3dMatrixManager)matrixManager).GlobalDr;
            Vector pcgRhs = CalcInterfaceProblemRhs(matrixManager, flexibility, globalDr);

            Vector lagranges;
            if (!(previousLambda == null))
            {
                lagranges = previousLambda;
            }
            else
            {
                lagranges = Vector.CreateZero(systemOrder);
            }

            // Solve the interface problem using PCG algorithm
            var pcgBuilder = new PcgAlgorithm.Builder();
            pcgBuilder.MaxIterationsProvider = pcgSettings.MaxIterationsProvider;
            pcgBuilder.ResidualTolerance = pcgSettings.ConvergenceTolerance;
            pcgBuilder.Convergence = pcgSettings.ConvergenceStrategyFactory.CreateConvergenceStrategy(globalForcesNorm);
            PcgAlgorithm pcg = pcgBuilder.Build(); //TODO: perhaps use the pcg from the previous analysis if it has reorthogonalization.

            IterativeStatistics stats;
            if (!(previousLambda == null))
            {
                stats = pcg.Solve(pcgMatrix, pcgPreconditioner, pcgRhs, lagranges, false,
                  () => Vector.CreateZero(systemOrder));
            }
            else
            {
                stats = pcg.Solve(pcgMatrix, pcgPreconditioner, pcgRhs, lagranges, true,
                  () => Vector.CreateZero(systemOrder));
            }

            // Log statistics about PCG execution
            FetiDPInterfaceProblemUtilities.CheckConvergence(stats);
            logger.LogIterativeAlgorithm(stats.NumIterationsRequired, stats.ResidualNormRatioEstimation);
            return lagranges;
        }

        private Vector CalcInterfaceProblemRhs(IFetiDPMatrixManager matrixManager, IFetiDPFlexibilityMatrix flexibility,
            Vector globalDr)
        {
            // rhs = dr - FIrcTilde * inv(KccStarTilde) * fcStarTilde
            Vector fcStarTilde = matrixManager.CoarseProblemRhs;
            Vector temp = matrixManager.MultiplyInverseCoarseProblemMatrix(fcStarTilde);
            temp = flexibility.MultiplyFIrc(temp);
            return globalDr - temp;
        }
    }
}
