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
            var writer = new LinearAlgebra.Output.FullMatrixWriter();

            string pathRhs = @"C:\Users\Serafeim\Desktop\FETI-DP\Matrices\rhs.txt";
            new LinearAlgebra.Output.FullVectorWriter().WriteToFile(pcgRhs, pathRhs);

            //LinearAlgebra.LibrarySettings.LinearAlgebraProviders = LinearAlgebra.LinearAlgebraProviderChoice.MKL;
            Matrix FIrr = MultiplyWithIdentity(nL, nL, flexibility.MultiplyGlobalFIrr);
            string pathFIrr = @"C:\Users\Serafeim\Desktop\FETI-DP\Matrices\FIrr.txt";
            writer.WriteToFile(FIrr, pathFIrr);
            double detFIrr = FIrr.CalcDeterminant();
            (Matrix rrefFIrr, List<int> independentColsFIrr) = FIrr.ReducedRowEchelonForm();
            //LinearAlgebra.Triangulation.CholeskyFull FIrrFactorized = FIrr.FactorCholesky(false);

            Matrix pcgMatrixExplicit = MultiplyWithIdentity(nL, nL, pcgMatrix.Multiply);
            pcgMatrixExplicit = 0.5 * (pcgMatrixExplicit + pcgMatrixExplicit.Transpose());
            double detPcgMatrix = pcgMatrixExplicit.CalcDeterminant();
            string pathPcgMatrix = @"C:\Users\Serafeim\Desktop\FETI-DP\Matrices\pcg_matrix.txt";
            writer.WriteToFile(pcgMatrixExplicit, pathPcgMatrix);
            (Matrix rref, List<int> independentCols) = pcgMatrixExplicit.ReducedRowEchelonForm();
            //LinearAlgebra.Triangulation.CholeskyFull pcgMatrixFactorized = pcgMatrixExplicit.FactorCholesky(false);

            try
            {

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
            }
            catch(Exception)
            { }
            lagranges.Clear();
            //pcgMatrixExplicit.FactorLU(false).SolveLinearSystem(pcgRhs, lagranges);

            lagranges.CopyFrom(Vector.CreateFromArray(new double[]
            {
                858.748624916974         ,
                6658.05367012006         ,
                -399.428687085375        ,
                -1528.25388011689        ,
                402.170912423947         ,
                2314.52006639940         ,
                2683.20368198754         ,
                -7135.53137052282        ,
                9070.92235542399         ,
                13842.1970399118         ,
                -1880.99413465743        ,
                9678.93855793406         ,
                10338.3227678329         ,
                -13002.0488673263        ,
                1097.57708574923         ,
                -694.660779359481        ,
                -1383.21562923371        ,
                -1406.01641958617        ,
                -38.0786491313622        ,
                12921.4673094043         ,
                -706.978302882614        ,
                87.3749802470211         ,
                711.175939111619         ,
                715.385199534560         ,
                -337.480062433103        ,
                -195.752169307628        ,
                -1954.92214461590        ,
                327.439313633485         ,
                154.553555829834         ,
                -9968.34410353963        ,
                -2777.05981113129        ,
                1948.71378858320         ,
                -11498.8174290606        ,
                -2976.07581401165        ,
                -21796.2688097026        ,
                -1629.01296236435        ,
                -481.032488066478        ,
                1078.15667667778         ,
                1116.63563428259         ,
                -215.797909226251        ,
                22156.3180836796         ,
1.72080596009070e-12
        }));

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
