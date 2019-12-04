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

            suite.AddFact(FetiDPDofSeparatorMpiTests.TestDofSeparation, typeof(FetiDPDofSeparatorMpiTests).Name, "TestDofSeparation");
            suite.AddFact(FetiDPDofSeparatorMpiTests.TestCornerBooleanMatrices, typeof(FetiDPDofSeparatorMpiTests).Name, "TestCornerBooleanMatrices");
            suite.AddFact(LagrangeMultiplierEnumeratorMpiTests.TestBooleanMappingMatrices, typeof(LagrangeMultiplierEnumeratorMpiTests).Name, "TestBooleanMappingMatrices");
            suite.AddFact(HomogeneousStiffnessDistributionMpiTests.TestBooleanMappingMatrices, typeof(HomogeneousStiffnessDistributionMpiTests).Name, "TestBooleanMappingMatrices");

            suite.AddFact(FetiDPMatrixManagerMpiTests.TestVectorsFbcFr, typeof(FetiDPMatrixManagerMpiTests).Name, "TestVectorsFbcFr");
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestMatricesKccKcrKrr, typeof(FetiDPMatrixManagerMpiTests).Name, "TestMatricesKccKcrKrr", MatrixFormat.Dense);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestMatricesKccKcrKrr, typeof(FetiDPMatrixManagerMpiTests).Name, "TestMatricesKccKcrKrr", MatrixFormat.Skyline);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestMatricesKccKcrKrr, typeof(FetiDPMatrixManagerMpiTests).Name, "TestMatricesKccKcrKrr", MatrixFormat.SuiteSparse);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestMatricesKbbKbiKii, typeof(FetiDPMatrixManagerMpiTests).Name, "TestMatricesKbbKbiKii", MatrixFormat.Dense);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestMatricesKbbKbiKii, typeof(FetiDPMatrixManagerMpiTests).Name, "TestMatricesKbbKbiKii", MatrixFormat.Skyline);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestMatricesKbbKbiKii, typeof(FetiDPMatrixManagerMpiTests).Name, "TestMatricesKbbKbiKii", MatrixFormat.SuiteSparse);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestStaticCondensations, typeof(FetiDPMatrixManagerMpiTests).Name, "TestStaticCondensations", MatrixFormat.Dense);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestStaticCondensations, typeof(FetiDPMatrixManagerMpiTests).Name, "TestStaticCondensations", MatrixFormat.Skyline);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestStaticCondensations, typeof(FetiDPMatrixManagerMpiTests).Name, "TestStaticCondensations", MatrixFormat.SuiteSparse);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestCoarseProblemMatrixAndRhs, typeof(FetiDPMatrixManagerMpiTests).Name, "TestCoarseProblemMatrixAndRhs", MatrixFormat.Dense);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestCoarseProblemMatrixAndRhs, typeof(FetiDPMatrixManagerMpiTests).Name, "TestCoarseProblemMatrixAndRhs", MatrixFormat.Skyline);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestCoarseProblemMatrixAndRhs, typeof(FetiDPMatrixManagerMpiTests).Name, "TestCoarseProblemMatrixAndRhs", MatrixFormat.SuiteSparse);

            suite.AddFact(FetiDPFlexibilityMatrixMpiTests.TestFIrcTimesVector, typeof(FetiDPFlexibilityMatrixMpiTests).Name, "TestFIrcTimesVector");
            suite.AddFact(FetiDPFlexibilityMatrixMpiTests.TestFIrcTransposedTimesVector, typeof(FetiDPFlexibilityMatrixMpiTests).Name, "TestFIrcTransposedTimesVector");
            suite.AddFact(FetiDPFlexibilityMatrixMpiTests.TestFIrrTimesVector, typeof(FetiDPFlexibilityMatrixMpiTests).Name, "TestFIrrTimesVector");
            suite.AddFact(FetiDPFlexibilityMatrixMpiTests.TestFIrrAndFIrcTransposedTimesVector, typeof(FetiDPFlexibilityMatrixMpiTests).Name, "TestFIrrAndFIrcTransposedTimesVector");

            suite.AddFact(FetiDPPreconditionerMpiTests.TestLumpedPreconditioner, typeof(FetiDPPreconditionerMpiTests).Name, "TestLumpedPreconditioner");
            suite.AddFact(FetiDPPreconditionerMpiTests.TestDirichletPreconditioner, typeof(FetiDPPreconditionerMpiTests).Name, "TestDirichletPreconditioner");
            suite.AddFact(FetiDPPreconditionerMpiTests.TestDiagonalDirichletPreconditioner, typeof(FetiDPPreconditionerMpiTests).Name, "TestDiagonalDirichletPreconditioner");

            suite.AddFact(FetiDPInterfaceProblemMpiTests.TestVectorDr, typeof(FetiDPInterfaceProblemMpiTests).Name, "TestVectorDr");
            suite.AddFact(FetiDPInterfaceProblemMpiTests.TestInterfaceProblemMatrix, typeof(FetiDPInterfaceProblemMpiTests).Name, "TestInterfaceProblemMatrix");
            suite.AddFact(FetiDPInterfaceProblemMpiTests.TestInterfaceProblemRhs, typeof(FetiDPInterfaceProblemMpiTests).Name, "TestInterfaceProblemRhs");
            suite.AddFact(FetiDPInterfaceProblemMpiTests.TestInterfaceProblemSolution, typeof(FetiDPInterfaceProblemMpiTests).Name, "TestInterfaceProblemSolution");

            suite.AddFact(FetiDPDisplacementsCalculatorMpiTests.TestCornerDisplacements, typeof(FetiDPDisplacementsCalculatorMpiTests).Name, "TestCornerDisplacements");
            suite.AddFact(FetiDPDisplacementsCalculatorMpiTests.TestFreeDisplacements, typeof(FetiDPDisplacementsCalculatorMpiTests).Name, "TestFreeDisplacements");

            suite.AddFact(FetiDPSubdomainGlobalMappingMpiTests.TestGlobalDiplacements, typeof(FetiDPSubdomainGlobalMappingMpiTests).Name, "TestGlobalDiplacements");
            suite.AddFact(FetiDPSubdomainGlobalMappingMpiTests.TestGlobalForcesNorm, typeof(FetiDPSubdomainGlobalMappingMpiTests).Name, "TestGlobalForcesNorm");

            suite.AddTheory(FetiDPSolverMpiTests.TestSolutionSubdomainDisplacements, typeof(FetiDPSolverMpiTests).Name, "TestSolutionSubdomainDisplacements", MatrixFormat.Skyline);
            suite.AddTheory(FetiDPSolverMpiTests.TestSolutionSubdomainDisplacements, typeof(FetiDPSolverMpiTests).Name, "TestSolutionSubdomainDisplacements", MatrixFormat.SuiteSparse);
            suite.AddTheory(FetiDPSolverMpiTests.TestSolutionGlobalDisplacements, typeof(FetiDPSolverMpiTests).Name, "TestSolutionGlobalDisplacements", MatrixFormat.Skyline);
            suite.AddTheory(FetiDPSolverMpiTests.TestSolutionGlobalDisplacements, typeof(FetiDPSolverMpiTests).Name, "TestSolutionGlobalDisplacements", MatrixFormat.SuiteSparse);

            suite.RunTests(args);
        }

        private static void RunTestsWith8MpiProcesses(string[] args)
        {
            var suite = new MpiTestSuite();

            suite.AddTheory(PapagiannakisFetiDPTests2DMpi.Run, typeof(PapagiannakisFetiDPTests2DMpi).Name, "Run", 1.0, Precond.Dirichlet, Residual.Approximate, 11, MatrixFormat.Skyline);
            suite.AddTheory(PapagiannakisFetiDPTests2DMpi.Run, typeof(PapagiannakisFetiDPTests2DMpi).Name, "Run", 1.0, Precond.Dirichlet, Residual.Approximate, 11, MatrixFormat.SuiteSparse);
            suite.AddTheory(PapagiannakisFetiDPTests2DMpi.Run, typeof(PapagiannakisFetiDPTests2DMpi).Name, "Run", 1.0, Precond.DirichletDiagonal, Residual.Approximate, 14, MatrixFormat.Skyline);
            suite.AddTheory(PapagiannakisFetiDPTests2DMpi.Run, typeof(PapagiannakisFetiDPTests2DMpi).Name, "Run", 1.0, Precond.DirichletDiagonal, Residual.Approximate, 14, MatrixFormat.SuiteSparse);
            suite.AddTheory(PapagiannakisFetiDPTests2DMpi.Run, typeof(PapagiannakisFetiDPTests2DMpi).Name, "Run", 1.0, Precond.Lumped, Residual.Approximate, 18, MatrixFormat.Skyline);
            suite.AddTheory(PapagiannakisFetiDPTests2DMpi.Run, typeof(PapagiannakisFetiDPTests2DMpi).Name, "Run", 1.0, Precond.Lumped, Residual.Approximate, 18, MatrixFormat.SuiteSparse);

            suite.RunTests(args);
        }
    }
}
