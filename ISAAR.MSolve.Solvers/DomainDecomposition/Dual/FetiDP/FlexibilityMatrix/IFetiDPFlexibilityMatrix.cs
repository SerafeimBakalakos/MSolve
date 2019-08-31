using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.FlexibilityMatrix
{
    public interface IFetiDPFlexibilityMatrix
    {
        int NumGlobalCornerDofs { get; }
        int NumGlobalLagrangeMultipliers { get; }

        void MultiplyGlobalFIrc(Vector lhs, Vector rhs);
        void MultiplyGlobalFIrcTransposed(Vector lhs, Vector rhs);
        void MultiplyGlobalFIrr(Vector lhs, Vector rhs);
    }
}
