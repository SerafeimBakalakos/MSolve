using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.MPI;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.UnitTests.Mocks;
using Xunit;

//TODO: Check with an example that has more than a single force applies to it.
namespace ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.UnitTests
{
    public static class FetiDPSubdomainGlobalMappingMpiTests
    {
        public static void TestGlobalDiplacements()
        {
            (ProcessDistribution procs, IModel model, FetiDPDofSeparatorMpi dofSeparator) =
                FetiDPDofSeparatorMpiTests.CreateModelAndDofSeparator();
            var stiffnessDistribution = new MockHomogeneousStiffnessDistribution();

            var mapping = new FetiDPSubdomainGlobalMappingMpi(procs, model, dofSeparator, stiffnessDistribution);
            Vector globalU = mapping.GatherGlobalDisplacements(
                sub => Example4x4QuadsHomogeneous.GetSolutionFreeDisplacements(sub.ID));

            if (procs.IsMasterProcess)
            {
                double tol = 1E-7;
                Assert.True(Example4x4QuadsHomogeneous.SolutionGlobalDisplacements.Equals(globalU, tol));
            }
        }

        public static void TestGlobalForcesNorm()
        {
            (ProcessDistribution procs, IModel model, FetiDPDofSeparatorMpi dofSeparator) =
                FetiDPDofSeparatorMpiTests.CreateModelAndDofSeparator();
            var stiffnessDistribution = new MockHomogeneousStiffnessDistribution();

            var mapping = new FetiDPSubdomainGlobalMappingMpi(procs, model, dofSeparator, stiffnessDistribution);
            double normF = mapping.CalcGlobalForcesNorm(sub => Example4x4QuadsHomogeneous.GetVectorFf(sub.ID));

            Assert.Equal(Example4x4QuadsHomogeneous.GlobalForcesNorm, normF, 10);
            //if (procs.IsMasterProcess) Assert.Equal(Example4x4QuadsHomogeneous.GlobalForcesNorm, normF, 10);
        }
    }
}
