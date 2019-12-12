using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Distributed.Tests;
using ISAAR.MSolve.LinearAlgebra.Tests.Utilities;
using ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.IntegrationTests;
using ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.UnitTests;
using ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.Utilities;
using ISAAR.MSolve.Solvers.Tests.Utilities;
using static ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.IntegrationTests.PapagiannakisFetiDPTests2DMpi;
using static ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.UnitTests.FetiDPMatrixManagerSerialTests;

// 1) Debug these 2) Replace MpiUtilities with the Vector and Matrix transferrer in other classes. 3) Delete MpiUtilities
namespace ISAAR.MSolve.Solvers.Tests
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            RunTestsWith4MpiProcesses(args);
            //RunTestsWith8MpiProcesses(args);
        }

        private static void RunTestsWith4MpiProcesses(string[] args)
        {
            var suite = new MpiTestSuite();

            suite.AddFact(FetiDPDofSeparatorMpiTests.TestDofSeparation);
            suite.AddFact(FetiDPDofSeparatorMpiTests.TestCornerBooleanMatrices);
            suite.AddFact(LagrangeMultiplierEnumeratorMpiTests.TestBooleanMappingMatrices);
            suite.AddFact(HomogeneousStiffnessDistributionMpiTests.TestBooleanMappingMatrices);

            suite.AddFact(FetiDPMatrixManagerMpiTests.TestVectorsFbcFr);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestMatricesKccKcrKrr, MatrixFormat.Dense);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestMatricesKccKcrKrr, MatrixFormat.Skyline);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestMatricesKccKcrKrr, MatrixFormat.SuiteSparse);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestMatricesKbbKbiKii, MatrixFormat.Dense);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestMatricesKbbKbiKii, MatrixFormat.Skyline);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestMatricesKbbKbiKii, MatrixFormat.SuiteSparse);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestStaticCondensations, MatrixFormat.Dense);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestStaticCondensations, MatrixFormat.Skyline);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestStaticCondensations, MatrixFormat.SuiteSparse);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestCoarseProblemMatrixAndRhs, MatrixFormat.Dense);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestCoarseProblemMatrixAndRhs, MatrixFormat.Skyline);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestCoarseProblemMatrixAndRhs, MatrixFormat.SuiteSparse);

            suite.AddFact(FetiDPFlexibilityMatrixMpiTests.TestFIrcTimesVector);
            suite.AddFact(FetiDPFlexibilityMatrixMpiTests.TestFIrcTransposedTimesVector);
            suite.AddFact(FetiDPFlexibilityMatrixMpiTests.TestFIrrTimesVector);
            suite.AddFact(FetiDPFlexibilityMatrixMpiTests.TestFIrrAndFIrcTransposedTimesVector);

            suite.AddFact(FetiDPPreconditionerMpiTests.TestLumpedPreconditioner);
            suite.AddFact(FetiDPPreconditionerMpiTests.TestDirichletPreconditioner);
            suite.AddFact(FetiDPPreconditionerMpiTests.TestDiagonalDirichletPreconditioner);

            suite.AddFact(FetiDPInterfaceProblemMpiTests.TestVectorDr);
            suite.AddFact(FetiDPInterfaceProblemMpiTests.TestInterfaceProblemMatrix);
            suite.AddFact(FetiDPInterfaceProblemMpiTests.TestInterfaceProblemRhs);
            suite.AddFact(FetiDPInterfaceProblemMpiTests.TestInterfaceProblemSolution);

            suite.AddFact(FetiDPDisplacementsCalculatorMpiTests.TestCornerDisplacements);
            suite.AddFact(FetiDPDisplacementsCalculatorMpiTests.TestFreeDisplacements);

            suite.AddFact(FetiDPSubdomainGlobalMappingMpiTests.TestGlobalDiplacements);
            suite.AddFact(FetiDPSubdomainGlobalMappingMpiTests.TestGlobalForcesNorm);

            suite.AddTheory(FetiDPSolverMpiTests.TestSolutionSubdomainDisplacements, MatrixFormat.Skyline);
            suite.AddTheory(FetiDPSolverMpiTests.TestSolutionSubdomainDisplacements, MatrixFormat.SuiteSparse);
            suite.AddTheory(FetiDPSolverMpiTests.TestSolutionGlobalDisplacements, MatrixFormat.Skyline);
            suite.AddTheory(FetiDPSolverMpiTests.TestSolutionGlobalDisplacements, MatrixFormat.SuiteSparse);

            suite.RunTests(args);
        }

        private static void RunTestsWith8MpiProcesses(string[] args)
        {
            var suite = new MpiTestSuite();

            suite.AddTheory(PapagiannakisFetiDPTests2DMpi.Run, 1.0, Precond.Dirichlet, Residual.Approximate, 11, MatrixFormat.Skyline);
            suite.AddTheory(PapagiannakisFetiDPTests2DMpi.Run, 1.0, Precond.Dirichlet, Residual.Approximate, 11, MatrixFormat.SuiteSparse);
            suite.AddTheory(PapagiannakisFetiDPTests2DMpi.Run, 1.0, Precond.DirichletDiagonal, Residual.Approximate, 14, MatrixFormat.Skyline);
            suite.AddTheory(PapagiannakisFetiDPTests2DMpi.Run, 1.0, Precond.DirichletDiagonal, Residual.Approximate, 14, MatrixFormat.SuiteSparse);
            suite.AddTheory(PapagiannakisFetiDPTests2DMpi.Run, 1.0, Precond.Lumped, Residual.Approximate, 18, MatrixFormat.Skyline);
            suite.AddTheory(PapagiannakisFetiDPTests2DMpi.Run, 1.0, Precond.Lumped, Residual.Approximate, 18, MatrixFormat.SuiteSparse);

            suite.RunTests(args);
        }
    }
}
