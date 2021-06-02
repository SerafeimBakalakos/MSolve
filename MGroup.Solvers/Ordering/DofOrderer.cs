using System;
using System.Collections.Generic;
using System.Linq;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Reordering;
using MGroup.Solvers.Ordering.Reordering;

namespace MGroup.Solvers.Ordering
{
    /// <summary>
    /// Orders the unconstrained freedom degrees of each subdomain and the shole model. Also applies any reordering and other 
    /// optimizations.
    /// </summary>
    public class DofOrderer : IDofOrderer
    {
        //TODO: this should also be a strategy, so that I could have caching with fallbacks, in case of insufficient memor.
        private readonly bool cacheElementToSubdomainDofMaps = true; 
        private readonly ConstrainedDofOrderingStrategy constrainedOrderingStrategy;
        private readonly IFreeDofOrderingStrategy freeOrderingStrategy;
        private readonly IDofReorderingStrategy reorderingStrategy;

        public DofOrderer()
            : this(new NodeMajorDofOrderingStrategy(), new NullReordering(), true)
        {
        }

        public DofOrderer(IDofReorderingStrategy reorderingStrategy, bool cacheElementToSubdomainDofMaps = true)
            : this(new NodeMajorDofOrderingStrategy(), reorderingStrategy, cacheElementToSubdomainDofMaps)
        {
        }

        public DofOrderer(IFreeDofOrderingStrategy freeOrderingStrategy, IDofReorderingStrategy reorderingStrategy,
            bool cacheElementToSubdomainDofMaps = true)
        {
            this.constrainedOrderingStrategy = new ConstrainedDofOrderingStrategy();
            this.freeOrderingStrategy = freeOrderingStrategy;
            this.reorderingStrategy = reorderingStrategy;
            this.cacheElementToSubdomainDofMaps = cacheElementToSubdomainDofMaps;
        }

        public ISubdomainConstrainedDofOrdering OrderConstrainedDofs(ISubdomain subdomain)
        {
            (int numConstrainedDofs, DofTable constrainedDofs) = 
                constrainedOrderingStrategy.OrderSubdomainDofs(subdomain);
            if (cacheElementToSubdomainDofMaps)
            {
                return new SubdomainConstrainedDofOrderingCaching(numConstrainedDofs, constrainedDofs);
            }
            else return new SubdomainConstrainedDofOrderingGeneral(numConstrainedDofs, constrainedDofs);
        }

        public ISubdomainFreeDofOrdering OrderFreeDofs(ISubdomain subdomain)
        {
            // Order subdomain dofs
            (int numSubdomainFreeDofs, DofTable subdomainFreeDofs) = freeOrderingStrategy.OrderSubdomainDofs(subdomain);
            ISubdomainFreeDofOrdering subdomainOrdering;
            if (cacheElementToSubdomainDofMaps)
            {
                subdomainOrdering = new SubdomainFreeDofOrderingCaching(numSubdomainFreeDofs, subdomainFreeDofs);
            }
            else subdomainOrdering = new SubdomainFreeDofOrderingGeneral(numSubdomainFreeDofs, subdomainFreeDofs);

            // Reorder subdomain dofs
            reorderingStrategy.ReorderDofs(subdomain, subdomainOrdering);

            return subdomainOrdering;
        }
    }
}
