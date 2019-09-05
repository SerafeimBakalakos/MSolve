using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Pcg;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Preconditioning;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.StiffnessDistribution;
using ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.UnitTests.Mocks;
using ISAAR.MSolve.Solvers.Tests.Utilities;
using Xunit;

//TODO: Mock all other FETI classes.
namespace ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.UnitTests
{
    public static class FetiDPPreconditionerMpiTests
    {
        [Fact]
        public static void TestDiagonalDirichletPreconditioner()
        {
            (ProcessDistribution procs, Matrix preconditioner) = CalcPreconditioner(new DiagonalDirichletPreconditioning());
            if (procs.IsMasterProcess)
            {
                double tol = 1E-13;
                Assert.True(Example4x4QuadsHomogeneous.MatrixPreconditionerDiagonalDirichlet.Equals(preconditioner, tol));
            }
        }

        [Fact]
        public static void TestDirichletPreconditioner()
        {
            (ProcessDistribution procs, Matrix preconditioner) = CalcPreconditioner(new DirichletPreconditioning());
            if (procs.IsMasterProcess)
            {
                double tol = 1E-13;
                Assert.True(Example4x4QuadsHomogeneous.MatrixPreconditionerDirichlet.Equals(preconditioner, tol));
            }
        }

        [Fact]
        public static void TestLumpedPreconditioner()
        {
            (ProcessDistribution procs, Matrix preconditioner) = CalcPreconditioner(new LumpedPreconditioning());
            if (procs.IsMasterProcess)
            {
                double tol = 1E-13;
                Assert.True(Example4x4QuadsHomogeneous.MatrixPreconditionerLumped.Equals(preconditioner, tol));
            }
        }

        private static (ProcessDistribution, Matrix) CalcPreconditioner(IFetiPreconditioningOperations preconditioning)
        {
            (ProcessDistribution procs, IModel model, FetiDPDofSeparatorMpi dofSeparator, 
                LagrangeMultipliersEnumeratorMpi lagrangesEnumerator) =
                LagrangeMultiplierEnumeratorMpiTests.CreateModelDofSeparatorLagrangesEnumerator();

            IFetiDPMatrixManager matrixManager = new MockMatrixManager();
            IStiffnessDistribution stiffnessDistribution = new MockHomogeneousStiffnessDistribution();
            var preconditionerFactory = new FetiPreconditionerMpi.Factory(procs);
            IFetiPreconditioner preconditioner = preconditionerFactory.CreatePreconditioner(preconditioning,
                model, dofSeparator, lagrangesEnumerator, matrixManager, stiffnessDistribution);

            // Create explicit matrices that can be checked
            int order = lagrangesEnumerator.NumLagrangeMultipliers;
            Matrix M = ImplicitMatrixUtilities.MultiplyWithIdentityMpi(order, order, preconditioner.SolveLinearSystem);

            return (procs, M);
        }
    }
}
