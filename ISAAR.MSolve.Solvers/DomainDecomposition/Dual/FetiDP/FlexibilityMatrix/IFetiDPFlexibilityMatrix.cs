using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;

//TODO: This class should be safe to call methods from, regardless which process it is.
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.FlexibilityMatrix
{
    public interface IFetiDPFlexibilityMatrix
    {
        int NumGlobalLagrangeMultipliers { get; }

        Vector MultiplyGlobalFIrc(Vector vIn);
        Vector MultiplyGlobalFIrcTransposed(Vector vIn);
        void MultiplyGlobalFIrr(Vector vIn, Vector vOut);
    }
}
