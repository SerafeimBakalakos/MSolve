using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;

//TODO: This class should be safe to call methods from, regardless which process it is.
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.FlexibilityMatrix
{
    public interface IFetiDPFlexibilityMatrix
    {
        int NumGlobalCornerDofs { get; } //TODO: Perhaps this should be removed as it will fail for processes other than master and it is not needed anyway.
        int NumGlobalLagrangeMultipliers { get; }

        void MultiplyGlobalFIrc(Vector lhs, Vector rhs);
        void MultiplyGlobalFIrcTransposed(Vector lhs, Vector rhs);
        void MultiplyGlobalFIrr(Vector lhs, Vector rhs);
    }
}
