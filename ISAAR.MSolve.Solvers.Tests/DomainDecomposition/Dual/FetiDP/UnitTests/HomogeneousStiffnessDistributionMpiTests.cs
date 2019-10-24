using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessDistribution;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.StiffnessDistribution;
using Xunit;

//TODO: Mock all other classes.
namespace ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP.UnitTests
{
    public static class HomogeneousStiffnessDistributionMpiTests
    {
        public static void TestBooleanMappingMatrices()
        {
            (ProcessDistribution procs, IModel model, IFetiDPDofSeparator dofSeparator,
                LagrangeMultipliersEnumeratorMpi lagrangesEnumerator) = 
                FetiDPLagrangesEnumeratorMpiTests.CreateModelDofSeparatorLagrangesEnumerator();
            ISubdomain subdomain = model.GetSubdomain(procs.OwnSubdomainID);

            // Calculate Bpbr matrices
            var stiffnessDistribution = new HomogeneousStiffnessDistributionMpi(procs, model, dofSeparator,
                new FetiDPHomogeneousDistributionLoadScaling(dofSeparator));
            stiffnessDistribution.Update();
            SignedBooleanMatrixColMajor Bb = lagrangesEnumerator.GetBooleanMatrix(subdomain).GetColumns(
                dofSeparator.GetBoundaryDofIndices(subdomain), false);
            IMappingMatrix Bpbr =
                stiffnessDistribution.CalcBoundaryPreconditioningSignedBooleanMatrix(lagrangesEnumerator, subdomain, Bb);

            // Check Bpbr matrices
            double tol = 1E-13;
            Matrix explicitBpr = Bpbr.MultiplyRight(Matrix.CreateIdentity(Bpbr.NumColumns));
            Assert.True(Example4x4QuadsHomogeneous.GetMatrixBpbr(subdomain.ID).Equals(explicitBpr, tol));
        }
    }
}
