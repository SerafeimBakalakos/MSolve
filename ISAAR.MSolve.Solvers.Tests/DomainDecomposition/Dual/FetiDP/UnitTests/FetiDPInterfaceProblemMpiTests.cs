using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.FlexibilityMatrix;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.InterfaceProblem;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Pcg;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Preconditioning;
using ISAAR.MSolve.Solvers.Logging;
using ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.UnitTests.Mocks;
using ISAAR.MSolve.Solvers.Tests.Utilities;
using Xunit;

namespace ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.UnitTests
{
    public static class FetiDPInterfaceProblemMpiTests
    {
        public static void TestInterfaceProblemMatrix()
        {
            (ProcessDistribution procs, IModel model, FetiDPDofSeparatorMpi dofSeparator,
                LagrangeMultipliersEnumeratorMpi lagrangesEnumerator) =
                LagrangeMultiplierEnumeratorMpiTests.CreateModelDofSeparatorLagrangesEnumerator();

            IFetiDPMatrixManager matrixManager = new MockMatrixManager(model);
            IFetiDPFlexibilityMatrix flexibility = new MockFlexibilityMatrix();

            var interfaceMatrix = new FetiDPInterfaceProblemMatrixMpi(procs, matrixManager, flexibility);

            // Create explicit matrix that can be checked
            Matrix A = ImplicitMatrixUtilities.MultiplyWithIdentity(
                interfaceMatrix.NumRows, interfaceMatrix.NumColumns, interfaceMatrix.Multiply);

            if (procs.IsMasterProcess)
            {
                // Check
                double tol = 1E-9;
                Assert.True(Example4x4QuadsHomogeneous.InterfaceProblemMatrix.Equals(A, tol));
            }
        }

        public static void TestInterfaceProblemSolution()
        {
            (ProcessDistribution procs, IModel model, FetiDPDofSeparatorMpi dofSeparator,
                LagrangeMultipliersEnumeratorMpi lagrangesEnumerator) =
                LagrangeMultiplierEnumeratorMpiTests.CreateModelDofSeparatorLagrangesEnumerator();
            IFetiDPMatrixManager matrixManager = new MockMatrixManager(model);
            var stiffnessDistribution = new MockHomogeneousStiffnessDistribution();
            IFetiDPFlexibilityMatrix flexibility = new MockFlexibilityMatrix();
            var precondFactory = new FetiPreconditionerMpi.Factory(procs);
            IFetiPreconditioner preconditioner = precondFactory.CreatePreconditioner(new DirichletPreconditioning(), model,
                dofSeparator, lagrangesEnumerator, matrixManager, stiffnessDistribution);

            var pcgSettings = new PcgSettings { ConvergenceTolerance = 1E-15 };
            var interfaceSolver = new FetiDPInterfaceProblemSolverMpi(procs, model, pcgSettings);
            Vector lagranges = interfaceSolver.SolveInterfaceProblem(matrixManager,
                lagrangesEnumerator, flexibility, preconditioner, Example4x4QuadsHomogeneous.GlobalForcesNorm,
                new SolverLoggerMpi(procs, "Test method"));

            if (procs.IsMasterProcess)
            {
                double tol = 1E-11;
                Assert.True(Example4x4QuadsHomogeneous.SolutionLagrangeMultipliers.Equals(lagranges, tol));
            }
        }

        public static void TestVectorDr()
        {
            (ProcessDistribution procs, IModel model, FetiDPDofSeparatorMpi dofSeparator, 
                LagrangeMultipliersEnumeratorMpi lagrangesEnumerator) =
                LagrangeMultiplierEnumeratorMpiTests.CreateModelDofSeparatorLagrangesEnumerator();
            IFetiDPMatrixManager matrixManager = new MockMatrixManager(model);

            var interfaceSolver = new FetiDPInterfaceProblemSolverMpi(procs, model, new PcgSettings());
            MethodInfo method = interfaceSolver.GetType().GetMethod("CalcGlobalDr",
                BindingFlags.NonPublic | BindingFlags.Instance); // reflection for the private method
            Vector globalDr = (Vector)method.Invoke(interfaceSolver, new object[] { matrixManager, lagrangesEnumerator });

            if (procs.IsMasterProcess)
            {
                double tol = 1E-13;
                Assert.True(Example4x4QuadsHomogeneous.VectorDr.Equals(globalDr, tol));
            }
        }
    }
}
