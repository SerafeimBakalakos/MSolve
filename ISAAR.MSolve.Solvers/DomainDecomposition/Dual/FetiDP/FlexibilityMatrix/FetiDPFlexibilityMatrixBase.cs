using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Commons;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.FlexibilityMatrix
{
    public abstract class FetiDPFlexibilityMatrixBase : IFetiDPFlexibilityMatrix
    {
        protected FetiDPFlexibilityMatrixBase(IFetiDPDofSeparator dofSeparator, 
            ILagrangeMultipliersEnumerator lagrangeEnumerator)
        {
            this.NumGlobalCornerDofs = dofSeparator.NumGlobalCornerDofs;
            this.NumGlobalLagrangeMultipliers = lagrangeEnumerator.NumLagrangeMultipliers;
        }

        protected delegate void CheckInput(Vector lhs, Vector rhs);
        protected delegate Vector CalcSubdomainContribution(FetiDPSubdomainFlexibilityMatrix subdomainFlexibility, Vector lhs);

        public int NumGlobalCornerDofs { get; }
        public int NumGlobalLagrangeMultipliers { get; } 

        public void MultiplyGlobalFIrc(Vector lhs, Vector rhs)
        {
            // FIrc[s] * x = sum_over_s( Br[s] * (inv(Krr[s]) * (Krc[s] * (Lc[s] * x))) )
            // This class only performs the summation.

            CheckInput checkInput = (vIn, vOut) =>
            {
                Preconditions.CheckMultiplicationDimensions(NumGlobalCornerDofs, vIn.Length);
                Preconditions.CheckSystemSolutionDimensions(NumGlobalLagrangeMultipliers, vOut.Length);
            };

            CalcSubdomainContribution calcSubdomainContribution = (subdomainFlexibility, vIn)
                => subdomainFlexibility.MultiplySubdomainFIrc(vIn);

            SumSubdomainContributions(lhs, rhs, checkInput, calcSubdomainContribution);
        }

        public void MultiplyGlobalFIrcTransposed(Vector lhs, Vector rhs)
        {
            // FIrc[s]^T * x = sum_over_s( Lc[s]^T * (Krc[s]^T * (inv(Krr[s]) * (Br[s]^T * x))) ) 
            // This class only performs the summation.

            CheckInput checkInput = (vIn, vOut) =>
            {
                Preconditions.CheckMultiplicationDimensions(NumGlobalLagrangeMultipliers, vIn.Length);
                Preconditions.CheckSystemSolutionDimensions(NumGlobalCornerDofs, vOut.Length);
            };

            CalcSubdomainContribution calcSubdomainContribution = (subdomainFlexibility, vIn)
                => subdomainFlexibility.MultiplySubdomainFIrcTransposed(vIn);

            SumSubdomainContributions(lhs, rhs, checkInput, calcSubdomainContribution);
        }

        public void MultiplyGlobalFIrr(Vector lhs, Vector rhs)
        {
            // FIrr[s] * x = sum_over_s( Br[s] * (inv(Krr[s]) * (Br[s]^T * x)) ) 
            // This class only performs the summation.

            CheckInput checkInput = (vIn, vOut) =>
            {
                Preconditions.CheckMultiplicationDimensions(NumGlobalLagrangeMultipliers, vIn.Length);
                Preconditions.CheckSystemSolutionDimensions(NumGlobalLagrangeMultipliers, vOut.Length);
            };
            
            CalcSubdomainContribution calcSubdomainContribution = (subdomainFlexibility, vIn) 
                => subdomainFlexibility.MultiplySubdomainFIrr(vIn);

            SumSubdomainContributions(lhs, rhs, checkInput, calcSubdomainContribution);
        }

        protected abstract void SumSubdomainContributions(Vector lhs, Vector rhs, CheckInput checkInput,
            CalcSubdomainContribution calcSubdomainContribution);
    }
}
