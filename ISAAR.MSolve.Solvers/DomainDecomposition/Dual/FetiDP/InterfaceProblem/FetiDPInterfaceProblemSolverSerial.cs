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
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Pcg;
using ISAAR.MSolve.Solvers.Logging;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.InterfaceProblem
{
    /// <summary>
    /// The interface problem is solved using PCG. The matrix of the coarse problem KccStar, namely the static condensation of 
    /// the remainder dofs onto the corner dofs is performed explicitly.
    /// </summary>
    public class FetiDPInterfaceProblemSolverSerial : IFetiDPInterfaceProblemSolver
    {
        private readonly IModel model;
        private readonly PcgSettings pcgSettings;

        public FetiDPInterfaceProblemSolverSerial(IModel model, PcgSettings pcgSettings)
        {
            this.model = model;
            this.pcgSettings = pcgSettings;
        }

        public Vector SolveInterfaceProblem(IFetiDPMatrixManager matrixManager,
            ILagrangeMultipliersEnumerator lagrangesEnumerator, IFetiDPFlexibilityMatrix flexibility, 
            IFetiPreconditioner preconditioner, double globalForcesNorm, ISolverLogger logger)
        {
            int systemOrder = flexibility.NumGlobalLagrangeMultipliers;

            // Prepare PCG matrix, preconditioner, rhs and solution
            var pcgMatrix = new FetiDPInterfaceProblemMatrixSerial(matrixManager, flexibility);
            var pcgPreconditioner = new FetiDPInterfaceProblemPreconditioner(preconditioner);
            Vector globalDr = CalcGlobalDr(matrixManager, lagrangesEnumerator);
            Vector pcgRhs = CalcInterfaceProblemRhs(matrixManager, flexibility, globalDr);
            var lagranges = Vector.CreateZero(systemOrder);

            #region debug
            int nL = lagranges.Length;
            int nC = matrixManager.CoarseProblemRhs.Length;
            //var writer = new LinearAlgebra.Output.FullMatrixWriter();

            //string pathRhs = @"C:\Users\Serafeim\Desktop\FETI-DP\Matrices\rhs.txt";
            ////new LinearAlgebra.Output.FullVectorWriter().WriteToFile(pcgRhs, pathRhs);
            ////LinearAlgebra.LibrarySettings.LinearAlgebraProviders = LinearAlgebra.LinearAlgebraProviderChoice.MKL;

            //// Process FIrr
            //Matrix FIrr = MultiplyWithIdentity(nL, nL, flexibility.MultiplyGlobalFIrr);
            //FIrr = 0.5 * (FIrr + FIrr.Transpose());
            //SkylineMatrix skyFIrr = SkylineMatrix.CreateFromMatrix(FIrr);
            //string pathFIrr = @"C:\Users\Serafeim\Desktop\FETI-DP\Matrices\FIrr.txt";
            ////writer.WriteToFile(FIrr, pathFIrr);
            //(Matrix rrefFIrr, List<int> independentColsFIrr) = FIrr.ReducedRowEchelonForm();

            //bool isFIrrInvertible = false;
            //double detFIrr = double.NaN;
            //try
            //{
            //    detFIrr = FIrr.CalcDeterminant();
            //    isFIrrInvertible = true;
            //}
            //catch (Exception) { }


            //bool isFIrrPosDef = false;
            //try
            //{
            //    double tol = 1E-50;
            //    var FIrrFactorized = skyFIrr.FactorCholesky(false, tol);
            //    isFIrrPosDef = true;
            //}
            //catch (Exception) { }


            //// Process PCG matrix
            //Matrix pcgMatrixExplicit = MultiplyWithIdentity(nL, nL, pcgMatrix.Multiply);
            //pcgMatrixExplicit = 0.5 * (pcgMatrixExplicit + pcgMatrixExplicit.Transpose());
            //SkylineMatrix skyPcgMatrix = SkylineMatrix.CreateFromMatrix(pcgMatrixExplicit);
            //string pathPcgMatrix = @"C:\Users\Serafeim\Desktop\FETI-DP\Matrices\pcg_matrix.txt";
            ////writer.WriteToFile(pcgMatrixExplicit, pathPcgMatrix);
            //(Matrix rref, List<int> independentCols) = pcgMatrixExplicit.ReducedRowEchelonForm();

            //bool isPcgMatrixInvertible = false;
            //double detPcgMatrix = double.NaN;
            //try
            //{
            //    detPcgMatrix = pcgMatrixExplicit.CalcDeterminant();
            //    isPcgMatrixInvertible = true;
            //}
            //catch (Exception) { }

            //bool isPcgMatrixPosDef = false;
            //try
            //{
            //    double tol = 1E-50;
            //    var pcgMatrixFactorized = skyPcgMatrix.FactorCholesky(false, tol);
            //    isPcgMatrixPosDef = true;
            //}
            //catch (Exception) { }
            #endregion

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

            #region debug
            int nIter = stats.NumIterationsRequired;

            //// Lagranges from LU
            //var lagrangesDirect = Vector.CreateZero(nL);
            //pcgMatrixExplicit.FactorLU(false).SolveLinearSystem(pcgRhs, lagrangesDirect);
            //double errorLagranges = (lagranges - lagrangesDirect).Norm2() / lagrangesDirect.Norm2();

            //return lagrangesDirect;
            #endregion
            return lagranges;
        }

        private Vector CalcInterfaceProblemRhs(IFetiDPMatrixManager matrixManager, IFetiDPFlexibilityMatrix flexibility,
            Vector globalDr)
        {
            // rhs = dr - FIrc * inv(KccStar) * fcStar
            Vector temp = matrixManager.MultiplyInverseCoarseProblemMatrix(matrixManager.CoarseProblemRhs);
            temp = flexibility.MultiplyGlobalFIrc(temp);
            return globalDr - temp;
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

        #region debug 
        public static Matrix MultiplyWithIdentity(int numRows, int numCols, Action<Vector, Vector> matrixVectorMultiplication)
        {
            var result = Matrix.CreateZero(numRows, numCols);
            for (int j = 0; j < numCols; ++j)
            {
                var lhs = Vector.CreateZero(numCols);
                lhs[j] = 1.0;
                var rhs = Vector.CreateZero(numRows);
                matrixVectorMultiplication(lhs, rhs);
                result.SetSubcolumn(j, rhs);
            }
            return result;
        }
        #endregion
    }
}
