using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.FlexibilityMatrix
{
    public class FetiDPFlexibilityMatrixSerial : FetiDPFlexibilityMatrixBase
    {
        private readonly Dictionary<ISubdomain, FetiDPSubdomainFlexibilityMatrix> subdomainFlexibilities;

        public FetiDPFlexibilityMatrixSerial(IModel model, IFetiDPDofSeparator dofSeparator, 
            ILagrangeMultipliersEnumerator lagrangeEnumerator, IFetiDPMatrixManager matrixManager) 
            : base(dofSeparator, lagrangeEnumerator)
        {
            subdomainFlexibilities = new Dictionary<ISubdomain, FetiDPSubdomainFlexibilityMatrix>();
            foreach (ISubdomain sub in model.EnumerateSubdomains())
            {
                subdomainFlexibilities[sub] = new FetiDPSubdomainFlexibilityMatrix(sub, dofSeparator, lagrangeEnumerator, 
                    matrixManager.GetSubdomainMatrixManager(sub));
            }
        }

        protected override void SumSubdomainContributions(Vector lhs, Vector rhs, CheckInput checkInput,
            CalcSubdomainContribution calcSubdomainContribution)
        {
            checkInput(lhs, rhs);
            rhs.Clear();
            foreach (ISubdomain sub in subdomainFlexibilities.Keys)
            {
                Vector subdomainRhs = calcSubdomainContribution(subdomainFlexibilities[sub], lhs);
                rhs.AddIntoThis(subdomainRhs);
            }
        }
    }
}
