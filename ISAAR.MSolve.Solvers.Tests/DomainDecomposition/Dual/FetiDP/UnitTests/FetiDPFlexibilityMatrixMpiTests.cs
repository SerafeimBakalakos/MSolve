using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.FlexibilityMatrix;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.UnitTests.Mocks;
using ISAAR.MSolve.Solvers.Tests.Utilities;
using Xunit;

namespace ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.UnitTests
{
    public static class FetiDPFlexibilityMatrixMpiTests
    {
        public static void TestFlexibilityMatrices() 
        {
            (ProcessDistribution procs, IModel model, FetiDPDofSeparatorMpi dofSeparator, 
                LagrangeMultipliersEnumeratorMpi lagrangesEnumerator) =
                FetiDPLagrangesEnumeratorMpiTests.CreateModelDofSeparatorLagrangesEnumerator();

            // Setup matrix manager
            IFetiDPMatrixManager matrixManager = new MockMatrixManager(model);

            // Create explicit matrices that can be checked
            var flexibility = new FetiDPFlexibilityMatrixMpi(procs, model, dofSeparator, lagrangesEnumerator, matrixManager);
            int numCornerDofs = dofSeparator.NumGlobalCornerDofs;
            int numLagranges = lagrangesEnumerator.NumLagrangeMultipliers;
            Matrix FIrr = ImplicitMatrixUtilities.MultiplyWithIdentityMpi(
                numLagranges, numLagranges, flexibility.MultiplyGlobalFIrr);
            Matrix FIrc = ImplicitMatrixUtilities.MultiplyWithIdentityMpi(
                numLagranges, numCornerDofs, flexibility.MultiplyGlobalFIrc);

            if (procs.IsMasterProcess)
            {
                // Check
                double tol = 1E-11;
                Assert.True(Example4x4QuadsHomogeneous.MatrixFIrr.Equals(FIrr, tol));
                Assert.True(Example4x4QuadsHomogeneous.MatrixFIrc.Equals(FIrc, tol));
            }
        }
    }
}
