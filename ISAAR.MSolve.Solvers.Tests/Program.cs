using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Tests.Utilities;
using ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.UnitTests;
using ISAAR.MSolve.Solvers.Tests.Utilities;
using static ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.UnitTests.FetiDPMatrixManagerSerialTests;

namespace ISAAR.MSolve.Solvers.Tests
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var suite = new MpiTestSuite();

            suite.AddFact(FetiDPDofSeparatorMpiTests.TestDofSeparation, typeof(FetiDPDofSeparatorMpiTests).Name, "TestDofSeparation");
            suite.AddFact(FetiDPDofSeparatorMpiTests.TestCornerBooleanMatrices, typeof(FetiDPDofSeparatorMpiTests).Name, "TestCornerBooleanMatrices");
            suite.AddFact(LagrangeMultiplierEnumeratorMpiTests.TestBooleanMappingMatrices, typeof(LagrangeMultiplierEnumeratorMpiTests).Name, "TestBooleanMappingMatrices");
            suite.AddFact(HomogeneousStiffnessDistributionMpiTests.TestBooleanMappingMatrices, typeof(HomogeneousStiffnessDistributionMpiTests).Name, "TestBooleanMappingMatrices");

            suite.AddFact(FetiDPMatrixManagerMpiTests.TestVectorsFbcFr, typeof(FetiDPMatrixManagerMpiTests).Name, "TestVectorsFbcFr");
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestMatricesKccKcrKrr, typeof(FetiDPMatrixManagerMpiTests).Name, "TestMatricesKccKcrKrr", MatrixFormat.Dense);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestMatricesKccKcrKrr, typeof(FetiDPMatrixManagerMpiTests).Name, "TestMatricesKccKcrKrr", MatrixFormat.Skyline);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestMatricesKbbKbiKii, typeof(FetiDPMatrixManagerMpiTests).Name, "TestMatricesKbbKbiKii", MatrixFormat.Dense);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestMatricesKbbKbiKii, typeof(FetiDPMatrixManagerMpiTests).Name, "TestMatricesKbbKbiKii", MatrixFormat.Skyline);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestStaticCondensations, typeof(FetiDPMatrixManagerMpiTests).Name, "TestStaticCondensations", MatrixFormat.Dense);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestStaticCondensations, typeof(FetiDPMatrixManagerMpiTests).Name, "TestStaticCondensations", MatrixFormat.Skyline);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestCoarseProblemMatrixAndRhs, typeof(FetiDPMatrixManagerMpiTests).Name, "TestCoarseProblemMatrixAndRhs", MatrixFormat.Dense);
            suite.AddTheory(FetiDPMatrixManagerMpiTests.TestCoarseProblemMatrixAndRhs, typeof(FetiDPMatrixManagerMpiTests).Name, "TestCoarseProblemMatrixAndRhs", MatrixFormat.Skyline);

            suite.AddFact(FetiDPFlexibilityMatrixMpiTests.TestFlexibilityMatrices, typeof(FetiDPFlexibilityMatrixMpiTests).Name, "TestFlexibilityMatrices");
            suite.AddFact(FetiDPPreconditionerMpiTests.TestLumpedPreconditioner, typeof(FetiDPPreconditionerMpiTests).Name, "TestLumpedPreconditioner");
            suite.AddFact(FetiDPPreconditionerMpiTests.TestDirichletPreconditioner, typeof(FetiDPPreconditionerMpiTests).Name, "TestDirichletPreconditioner");
            suite.AddFact(FetiDPPreconditionerMpiTests.TestDiagonalDirichletPreconditioner, typeof(FetiDPPreconditionerMpiTests).Name, "TestDiagonalDirichletPreconditioner");

            suite.AddFact(FetiDPInterfaceProblemMpiTests.TestVectorDr, typeof(FetiDPInterfaceProblemMpiTests).Name, "TestVectorDr");
            suite.AddFact(FetiDPInterfaceProblemMpiTests.TestInterfaceProblemMatrix, typeof(FetiDPInterfaceProblemMpiTests).Name, "TestInterfaceProblemMatrix");
            suite.AddFact(FetiDPInterfaceProblemMpiTests.TestInterfaceProblemSolution, typeof(FetiDPInterfaceProblemMpiTests).Name, "TestInterfaceProblemSolution");

            suite.AddFact(FetiDPDisplacementsCalculatorMpi.TestFreeDisplacements, typeof(FetiDPDisplacementsCalculatorMpi).Name, "TestFreeDisplacements");


            suite.RunTests(args);
        }
    }
}
