using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Iterative;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.FlexibilityMatrix;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.InterfaceProblem
{
    /// <summary>
    /// This class concerns global operations and uses global data. In an MPI environment, instances of it should not be used in 
    /// processes other than master. 
    /// </summary>
    public class FetiDPInterfaceProblemMatrixSerial : ILinearTransformation
    {
        private readonly IFetiDPFlexibilityMatrix flexibility;
        private readonly IFetiDPMatrixManager matrixManager;

        public FetiDPInterfaceProblemMatrixSerial(IFetiDPMatrixManager matrixManager, IFetiDPFlexibilityMatrix flexibility)
        {
            this.matrixManager = matrixManager;
            this.flexibility = flexibility;
        }

        public int NumColumns => flexibility.NumGlobalLagrangeMultipliers;
        public int NumRows => flexibility.NumGlobalLagrangeMultipliers;

        public void Multiply(IVectorView lhsVector, IVector rhsVector)
        {
            //TODO: remove casts. I think PCG, LinearTransformation and preconditioners should be generic, bounded by 
            //      IVectorView and IVector
            var lhs = (Vector)lhsVector;
            var rhs = (Vector)rhsVector;

            // rhs = (FIrr + FIrc * inv(KccStar) * FIrc^T) * lhs
            flexibility.MultiplyGlobalFIrr(lhs, rhs);

            Vector temp = flexibility.MultiplyGlobalFIrcTransposed(lhs);
            temp = matrixManager.MultiplyInverseCoarseProblemMatrix(temp);
            temp = flexibility.MultiplyGlobalFIrc(temp);
            rhs.AddIntoThis(temp);
        }
    }
}
