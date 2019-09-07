using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.Displacements;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.FlexibilityMatrix;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.UnitTests.Mocks;
using Xunit;

namespace ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.UnitTests
{
    public static class FetiDPDisplacementsCalculatorMpi
    {
        public static void TestFreeDisplacements()
        {
            (ProcessDistribution procs, IModel model, FetiDPDofSeparatorMpi dofSeparator, 
                LagrangeMultipliersEnumeratorMpi lagrangesEnumerator) =
                LagrangeMultiplierEnumeratorMpiTests.CreateModelDofSeparatorLagrangesEnumerator();
            IFetiDPMatrixManager matrixManager = new MockMatrixManager(model);
            IFetiDPFlexibilityMatrix flexibility = new MockFlexibilityMatrix();

            var displacementsCalculator = new FreeDofDisplacementsCalculatorMpi(procs, model, dofSeparator, matrixManager, 
                lagrangesEnumerator);
            Vector lagranges = Example4x4QuadsHomogeneous.SolutionLagrangeMultipliers;
            displacementsCalculator.CalculateSubdomainDisplacements(lagranges, flexibility);

            ISubdomain subdomain = model.GetSubdomain(procs.OwnSubdomainID);
            double tol = 1E-7;
            IVectorView uf = matrixManager.GetFetiDPSubdomainMatrixManager(subdomain).LinearSystem.Solution;
            Assert.True(Example4x4QuadsHomogeneous.GetSolutionFreeDisplacements(subdomain.ID).Equals(uf, tol));
        }
    }
}
