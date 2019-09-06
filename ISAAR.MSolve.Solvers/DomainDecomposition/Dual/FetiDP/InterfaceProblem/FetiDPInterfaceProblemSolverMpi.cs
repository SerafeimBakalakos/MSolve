﻿using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.Iterative;
using ISAAR.MSolve.LinearAlgebra.Iterative.PreconditionedConjugateGradient;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.FlexibilityMatrix;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Pcg;
using MPI;

//TODO: Reduce the duplication between MPI and serial implementations
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.InterfaceProblem
{
    /// <summary>
    /// The interface problem is solved using PCG. The matrix of the coarse problem KccStar, namely the static condensation of 
    /// the remainder dofs onto the corner dofs is performed explicitly.
    /// </summary>
    public class FetiDPInterfaceProblemSolverMpi : IFetiDPInterfaceProblemSolver
    {
        private readonly IModel model;
        private readonly PcgSettings pcgSettings;
        private readonly ProcessDistribution procs;

        public FetiDPInterfaceProblemSolverMpi(ProcessDistribution processDistribution, IModel model, PcgSettings pcgSettings)
        {
            this.procs = processDistribution;
            this.model = model;
            this.pcgSettings = pcgSettings;
        }

        public (Vector lagrangeMultipliers, Vector cornerDisplacements) SolveInterfaceProblem(IFetiDPMatrixManager matrixManager,
            ILagrangeMultipliersEnumerator lagrangesEnumerator, IFetiDPFlexibilityMatrix flexibility, 
            IFetiPreconditioner preconditioner, double globalForcesNorm, SolverLogger logger)
        {
            int systemOrder = flexibility.NumGlobalLagrangeMultipliers;

            // Prepare PCG matrix, preconditioner, rhs and solution
            var pcgMatrix = new FetiDPInterfaceProblemMatrixMpi(procs, matrixManager, flexibility);
            var pcgPreconditioner = new FetiDPInterfaceProblemPreconditioner(preconditioner);
            Vector pcgRhs = null;
            Vector lagranges = null;
            Vector globalDr = CalcGlobalDr(matrixManager, lagrangesEnumerator);
            if (procs.IsMasterProcess)
            {
                pcgRhs = FetiDPInterfaceProblemUtilities.CalcInterfaceProblemRhs(matrixManager, flexibility, globalDr);
                lagranges = Vector.CreateZero(systemOrder);
            }

            // Solve the interface problem using PCG algorithm
            var pcgBuilder = new PcgAlgorithmMpi.Builder();
            pcgBuilder.MaxIterationsProvider = pcgSettings.MaxIterationsProvider;
            pcgBuilder.ResidualTolerance = pcgSettings.ConvergenceTolerance;
            pcgBuilder.Convergence = pcgSettings.ConvergenceStrategyFactory.CreateConvergenceStrategy(globalForcesNorm);
            PcgAlgorithmMpi pcg = pcgBuilder.Build(procs.Communicator, procs.MasterProcess); //TODO: perhaps use the pcg from the previous analysis if it has reorthogonalization.
            IterativeStatistics stats = pcg.Solve(pcgMatrix, pcgPreconditioner, pcgRhs, lagranges, true,
                () => Vector.CreateZero(systemOrder));

            // Log statistics about PCG execution
            FetiDPInterfaceProblemUtilities.CheckConvergence(stats);
            if (procs.IsMasterProcess) logger.LogIterativeAlgorithm(stats.NumIterationsRequired,
                stats.ResidualNormRatioEstimation);

            // Calculate corner displacements
            Vector uc = null;
            if (procs.IsMasterProcess)
            {
                uc = FetiDPInterfaceProblemUtilities.CalcCornerDisplacements(matrixManager, flexibility, lagranges);
            }

            return (lagranges, uc);
        }

        private Vector CalcGlobalDr(IFetiDPMatrixManager matrixManager, ILagrangeMultipliersEnumerator lagrangesEnumerator)
        {
            ISubdomain subdomain = model.GetSubdomain(procs.OwnSubdomainID);
            Vector subdomainDr = FetiDPInterfaceProblemUtilities.CalcSubdomainDr(subdomain, matrixManager, lagrangesEnumerator);
            return procs.Communicator.SumVector(subdomainDr, procs.MasterProcess);
        }
    }
}