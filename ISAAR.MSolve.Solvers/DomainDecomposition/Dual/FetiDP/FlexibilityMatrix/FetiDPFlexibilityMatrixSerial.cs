using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.FlexibilityMatrix
{
    public class FetiDPFlexibilityMatrixSerial : FetiDPFlexibilityMatrixBase
    {
        private readonly FetiDPLagrangeMultipliersEnumerator lagrangeEnumerator;
        private readonly Dictionary<ISubdomain, FetiDPSubdomainFlexibilityMatrix> subdomainFlexibilities;

        public FetiDPFlexibilityMatrixSerial(IModel model, IFetiDPDofSeparator dofSeparator, 
            FetiDPLagrangeMultipliersEnumerator lagrangeEnumerator, IFetiDPMatrixManager matrixManager) 
            : base(dofSeparator)
        {
            this.lagrangeEnumerator = lagrangeEnumerator;

            this.NumGlobalLagrangeMultipliers = lagrangeEnumerator.NumLagrangeMultipliers;

            subdomainFlexibilities = new Dictionary<ISubdomain, FetiDPSubdomainFlexibilityMatrix>();
            foreach (ISubdomain sub in model.EnumerateSubdomains())
            {
                IFetiDPSubdomainMatrixManager subdomainMatrices = matrixManager.GetSubdomainMatrixManager(sub);
                subdomainFlexibilities[sub] = new FetiDPSubdomainFlexibilityMatrix(sub, dofSeparator, subdomainMatrices);
            }
        }

        public override int NumGlobalLagrangeMultipliers { get; }

        protected override void SumSubdomainContributions(Vector lhs, Vector rhs, CheckInput checkInput,
            CalcSubdomainContribution calcSubdomainContribution)
        {
            checkInput(lhs, rhs);
            rhs.Clear();
            foreach (ISubdomain sub in subdomainFlexibilities.Keys)
            {
                Vector subdomainRhs = 
                    calcSubdomainContribution(subdomainFlexibilities[sub], lhs, lagrangeEnumerator.BooleanMatrices[sub.ID]);

                rhs.AddIntoThis(subdomainRhs);
            }
        }
    }
}
