using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.FlexibilityMatrix;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.InterfaceProblem;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessDistribution;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Pcg;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Preconditioning;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.StiffnessDistribution;
using ISAAR.MSolve.Solvers.Logging;
using ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.UnitTests.Mocks;
using ISAAR.MSolve.Solvers.Tests.Utilities;
using Xunit;

namespace ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.UnitTests
{
    public static class FetiDPInterfaceProblemSerialTests
    {
        [Fact]
        public static void TestCornerDofDisplacements()
        {
            IFetiDPMatrixManager matrixManager = new MockMatrixManager();
            IFetiDPFlexibilityMatrix flexibility = new MockFlexibilityMatrix();
            Vector lagranges = Example4x4QuadsHomogeneous.SolutionLagrangeMultipliers;
            Vector cornerDisplacements = 
                FetiDPInterfaceProblemUtilities.CalcCornerDisplacements(matrixManager, flexibility, lagranges);

            double tol = 1E-12;
            Assert.True(Example4x4QuadsHomogeneous.SolutionCornerDisplacements.Equals(cornerDisplacements, tol));
        }

        [Fact]
        public static void TestInterfaceProblemMatrix()
        {
            IFetiDPMatrixManager matrixManager = new MockMatrixManager();
            IFetiDPFlexibilityMatrix flexibility = new MockFlexibilityMatrix();

            var interfaceMatrix = new FetiDPInterfaceProblemMatrixSerial(matrixManager, flexibility);

            // Create explicit matrix that can be checked
            Matrix A = ImplicitMatrixUtilities.MultiplyWithIdentity(
                interfaceMatrix.NumRows, interfaceMatrix.NumColumns, interfaceMatrix.Multiply);

            // Check
            double tol = 1E-9;
            Assert.True(Example4x4QuadsHomogeneous.InterfaceProblemMatrix.Equals(A, tol));
        }

        [Fact]
        public static void TestInterfaceProblemRhs()
        {
            IFetiDPMatrixManager matrixManager = new MockMatrixManager();
            IFetiDPFlexibilityMatrix flexibility = new MockFlexibilityMatrix();
            Vector globalDr = Example4x4QuadsHomogeneous.VectorDr;
            Vector pcgRhs = FetiDPInterfaceProblemUtilities.CalcInterfaceProblemRhs(matrixManager, flexibility, globalDr);

            double tol = 1E-11;
            Assert.True(Example4x4QuadsHomogeneous.InterfaceProblemRhs.Equals(pcgRhs, tol));
        }

        [Fact]
        public static void TestInterfaceProblemSolution()
        {
            (IModel model, FetiDPDofSeparatorSerial dofSeparator, LagrangeMultipliersEnumeratorSerial lagrangesEnumerator) =
                LagrangeMultiplierEnumeratorSerialTests.CreateModelDofSeparatorLagrangesEnumerator();
            IFetiDPMatrixManager matrixManager = new MockMatrixManager();
            var stiffnessDistribution = new MockHomogeneousStiffnessDistribution();
            IFetiDPFlexibilityMatrix flexibility = new MockFlexibilityMatrix();
            var precondFactory = new FetiPreconditionerSerial.Factory();
            IFetiPreconditioner preconditioner = precondFactory.CreatePreconditioner(new DirichletPreconditioning(), model,
                dofSeparator, lagrangesEnumerator, matrixManager, stiffnessDistribution);

            var pcgSettings = new PcgSettings { ConvergenceTolerance = 1E-15 };
            var interfaceSolver = new FetiDPInterfaceProblemSolverSerial(model, pcgSettings);
            (Vector lagranges, Vector cornerDisplacements) = interfaceSolver.SolveInterfaceProblem(matrixManager,
                lagrangesEnumerator, flexibility, preconditioner, Example4x4QuadsHomogeneous.GlobalForcesNorm, 
                new SolverLoggerSerial("Test method"));

            double tol = 1E-11;
            Assert.True(Example4x4QuadsHomogeneous.SolutionLagrangeMultipliers.Equals(lagranges, tol));
            Assert.True(Example4x4QuadsHomogeneous.SolutionCornerDisplacements.Equals(cornerDisplacements, tol));
        }

        [Fact]
        public static void TestVectorDr()
        {
            (IModel model, FetiDPDofSeparatorSerial dofSeparator, LagrangeMultipliersEnumeratorSerial lagrangesEnumerator) =
                LagrangeMultiplierEnumeratorSerialTests.CreateModelDofSeparatorLagrangesEnumerator();
            IFetiDPMatrixManager matrixManager = new MockMatrixManager();

            var interfaceSolver = new FetiDPInterfaceProblemSolverSerial(model, new PcgSettings());
            MethodInfo method = interfaceSolver.GetType().GetMethod("CalcGlobalDr",
                BindingFlags.NonPublic | BindingFlags.Instance); // reflection for the private method
            Vector globalDr = (Vector)method.Invoke(interfaceSolver, new object[] { matrixManager, lagrangesEnumerator });

            double tol = 1E-13;
            Assert.True(Example4x4QuadsHomogeneous.VectorDr.Equals(globalDr, tol));
        }
    }
}
