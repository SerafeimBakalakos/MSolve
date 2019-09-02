using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using Xunit;

//TODO: Mock all other classes.
namespace ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.UnitTests
{
    public static class LagrangeMultiplierEnumeratorMpiTests
    {
        public static void TestBooleanMappingMatrices()
        {
            (ProcessDistribution procs, IModel model, IFetiDPDofSeparator dofSeparator) = 
                FetiDPDofSeparatorMpiTests.CreateModelAndDofSeparator();
            ISubdomain subdomain = model.GetSubdomain(procs.OwnSubdomainID);

            // Enumerate lagranges and calculate the boolean matrices
            var crosspointStrategy = new FullyRedundantConstraints();
            var lagrangeEnumerator = new LagrangeMultipliersEnumeratorMpi(procs, model, crosspointStrategy, dofSeparator);
            lagrangeEnumerator.CalcBooleanMatrices(dofSeparator.GetRemainderDofOrdering);

            // Check
            double tolerance = 1E-13;
            Assert.Equal(8, lagrangeEnumerator.NumLagrangeMultipliers);
            Matrix Br = lagrangeEnumerator.GetBooleanMatrix(subdomain).CopyToFullMatrix(false);
            Matrix expectedBr = Example4x4QuadsHomogeneous.GetMatrixBr(subdomain.ID);
            Assert.True(expectedBr.Equals(Br, tolerance));
        }
    }
}
