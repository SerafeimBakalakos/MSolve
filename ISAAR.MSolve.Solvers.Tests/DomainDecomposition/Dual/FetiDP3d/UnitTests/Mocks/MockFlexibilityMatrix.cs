using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.FlexibilityMatrix;

namespace ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP3d.UnitTests.Mocks
{
    public class MockFlexibilityMatrix : IFetiDPFlexibilityMatrix
    {
        public int NumGlobalLagrangeMultipliers => Example4x4x4Quads.ExpectedConnectivityData.NumGlobalLagrangeMultipliers;

        public Vector MultiplyGlobalFIrc(Vector vIn)
        {
            if (vIn != null) return Example4x4x4Quads.ExpectedGlobalMatrices.MatrixFIrcTildeSimple * vIn;
            else return null;
        }

        public Vector MultiplyGlobalFIrcTransposed(Vector vIn)
        {
            if (vIn != null) return Example4x4x4Quads.ExpectedGlobalMatrices.MatrixFIrcTildeSimple.Multiply(vIn, true);
            else return null;
        }

        public void MultiplyGlobalFIrr(Vector vIn, Vector vOut)
        {
            if ((vIn != null) && (vOut != null)) vOut.CopyFrom(Example4x4x4Quads.ExpectedGlobalMatrices.MatrixFIrr * vIn);
        }
    }
}
