using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;

//TODO: Br should be accessed by IFetiDPLagrangeEnumerator.GetBr(ISubdomain)
//TODO: It should be injected IFetiDPMatrixManager and IFetiDPLagrangeEnumerator in the constructor. Both of these should follow PCW.
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.FlexibilityMatrix
{
    public class FetiDPSubdomainFlexibilityMatrix
    {
        private readonly IFetiDPDofSeparator dofSeparator;
        private readonly IFetiDPSubdomainMatrixManager matrixManager;
        private readonly ISubdomain subdomain;

        public FetiDPSubdomainFlexibilityMatrix(ISubdomain subdomain, IFetiDPDofSeparator dofSeparator, 
            IFetiDPSubdomainMatrixManager matrixManager)
        {
            this.subdomain = subdomain;
            this.matrixManager = matrixManager;
            this.dofSeparator = dofSeparator;
        }

        public Vector MultiplySubdomainFIrc(Vector vector, SignedBooleanMatrixColMajor Br)
        {
            // FIrc[s] * x = sum_over_s( Br[s] * (inv(Krr[s]) * (Krc[s] * (Lc[s] * x))) ) 
            // Summing is delegated to another class.
            // This class performs: fIrc[s] * x = Br[s] * (inv(Krr[s]) * (Krc[s] * (Lc[s] * x)))

            UnsignedBooleanMatrix Lc = dofSeparator.GetCornerBooleanMatrix(subdomain);
            Vector temp = Lc.Multiply(vector);
            temp = matrixManager.MultiplyKrcTimes(temp);
            temp = matrixManager.MultiplyInverseKrrTimes(temp);
            return Br.Multiply(temp);
        }

        public Vector MultiplySubdomainFIrcTransposed(Vector vector, SignedBooleanMatrixColMajor Br)
        {
            // FIrc[s]^T * x = sum_over_s( Lc[s]^T * (Krc[s]^T * (inv(Krr[s]) * (Br[s]^T * x))) ) 
            // Summing is delegated to another class.
            // This class performs: fIrc[s]^T * x = Lc[s]^T * (Krc[s]^T * (inv(Krr[s]) * (Br[s]^T * x)))

            UnsignedBooleanMatrix Lc = dofSeparator.GetCornerBooleanMatrix(subdomain);
            Vector temp = Br.Multiply(vector, true);
            temp = matrixManager.MultiplyInverseKrrTimes(temp);
            temp = matrixManager.MultiplyKcrTimes(temp);
            return Lc.Multiply(temp, true);
        }
        
        public Vector MultiplySubdomainFIrr(Vector vector, SignedBooleanMatrixColMajor Br)
        {
            // FIrr[s] * x = sum_over_s( Br[s] * (inv(Krr[s]) * (Br[s]^T * x)) ) 
            // Summing is delegated to another class.
            // This class performs: fIrr[s] * x = Br[s] * (inv(Krr[s]) * (Br[s]^T * x))

            Vector temp = Br.Multiply(vector, true);
            temp = matrixManager.MultiplyInverseKrrTimes(temp);
            return Br.Multiply(temp);
        }
    }
}
