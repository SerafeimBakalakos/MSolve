using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.Solvers.DomainDecomposition.DofSeparation;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers
{
    public class LagrangeMultipliersEnumeratorSerial : ILagrangeMultipliersEnumerator
    {
        private readonly ICrosspointStrategy crosspointStrategy;
        private readonly IDofSeparator dofSeparator;
        private readonly IModel model;

        private Dictionary<ISubdomain, SignedBooleanMatrixColMajor> subdomainBooleanMatrices;

        public LagrangeMultipliersEnumeratorSerial(IModel model, ICrosspointStrategy crosspointStrategy,
            IDofSeparator dofSeparator)
        {
            this.model = model;
            this.crosspointStrategy = crosspointStrategy;
            this.dofSeparator = dofSeparator;
        }

        public IReadOnlyList<LagrangeMultiplier> LagrangeMultipliers { get; private set; }

        public int NumLagrangeMultipliers { get; private set; }

        public SignedBooleanMatrixColMajor GetBooleanMatrix(ISubdomain subdomain) => subdomainBooleanMatrices[subdomain];

        public void CalcBooleanMatrices(Func<ISubdomain, DofTable> getSubdomainDofOrdering)
        {
            // Define the lagrange multipliers
            LagrangeMultipliers = 
                LagrangeMultipliersUtilities.DefineLagrangeMultipliers(dofSeparator.GlobalBoundaryDofs, crosspointStrategy);
            NumLagrangeMultipliers = LagrangeMultipliers.Count;

            // Define the subdomain dofs
            var subdomainDofOrderings = new Dictionary<ISubdomain, DofTable>();
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                subdomainDofOrderings[subdomain] = getSubdomainDofOrdering(subdomain);
            }

            // Calculate the boolean matrices
            subdomainBooleanMatrices = 
                LagrangeMultipliersUtilities.CalcAllBooleanMatrices(LagrangeMultipliers, subdomainDofOrderings);
        }
    }
}
