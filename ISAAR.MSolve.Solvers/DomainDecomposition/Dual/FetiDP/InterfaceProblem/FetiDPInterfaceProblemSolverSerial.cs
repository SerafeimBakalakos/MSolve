using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Iterative;
using ISAAR.MSolve.LinearAlgebra.Iterative.PreconditionedConjugateGradient;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.FlexibilityMatrix;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Pcg;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.InterfaceProblem
{
    /// <summary>
    /// The interface problem is solved using PCG. The matrix of the coarse problem KccStar, namely the static condensation of 
    /// the remainder dofs onto the corner dofs is performed explicitly.
    /// </summary>
    public class FetDPInterfaceProblemSolverSerial : IFetiDPInterfaceProblemSolver
    {
        private readonly IModel model;
        private readonly PcgSettings pcgSettings;

        public FetDPInterfaceProblemSolverSerial(IModel model, PcgSettings pcgSettings)
        {
            this.model = model;
            this.pcgSettings = pcgSettings;
        }

        public (Vector lagrangeMultipliers, Vector cornerDisplacements) SolveInterfaceProblem(IFetiDPMatrixManager matrixManager,
            ILagrangeMultipliersEnumerator lagrangesEnumerator, IFetiDPFlexibilityMatrix flexibility, 
            IFetiPreconditioner preconditioner, double globalForcesNorm, SolverLogger logger)
        {
            int systemOrder = flexibility.NumGlobalLagrangeMultipliers;

            // Prepare PCG matrix, preconditioner, rhs and solution
            var pcgMatrix = new FetiDPInterfaceProblemMatrix(matrixManager, flexibility);
            var pcgPreconditioner = new FetiDPInterfaceProblemPreconditioner(preconditioner);
            Vector globalDr = CalcGlobalDr(matrixManager, lagrangesEnumerator);
            Vector pcgRhs = FetiDPInterfaceProblemUtilities.CalcInterfaceProblemRhs(matrixManager, flexibility, globalDr);
            var lagranges = Vector.CreateZero(systemOrder);

            // Solve the interface problem using PCG algorithm
            var pcgBuilder = new PcgAlgorithm.Builder();
            pcgBuilder.MaxIterationsProvider = pcgSettings.MaxIterationsProvider;
            pcgBuilder.ResidualTolerance = pcgSettings.ConvergenceTolerance;
            pcgBuilder.Convergence = pcgSettings.ConvergenceStrategyFactory.CreateConvergenceStrategy(globalForcesNorm);
            PcgAlgorithm pcg = pcgBuilder.Build(); //TODO: perhaps use the pcg from the previous analysis if it has reorthogonalization.
            IterativeStatistics stats = pcg.Solve(pcgMatrix, pcgPreconditioner, pcgRhs, lagranges, true,
                () => Vector.CreateZero(systemOrder));

            // Log statistics about PCG execution
            FetiDPInterfaceProblemUtilities.CheckConvergence(stats);
            logger.LogIterativeAlgorithm(stats.NumIterationsRequired, stats.ResidualNormRatioEstimation);

            // Calculate corner displacements
            Vector uc = FetiDPInterfaceProblemUtilities.CalcCornerDisplacements(matrixManager, flexibility, lagranges);

            return (lagranges, uc);
        }

        private Vector CalcGlobalDr(IFetiDPMatrixManager matrixManager, ILagrangeMultipliersEnumerator lagrangesEnumerator)
        {
            var globalDr = Vector.CreateZero(lagrangesEnumerator.NumLagrangeMultipliers);
            foreach (ISubdomain sub in model.EnumerateSubdomains())
            {
                Vector subdomainDr = FetiDPInterfaceProblemUtilities.CalcSubdomainDr(sub, matrixManager, lagrangesEnumerator);
                globalDr.AddIntoThis(subdomainDr);
            }
            return globalDr;
        }
    }
}
