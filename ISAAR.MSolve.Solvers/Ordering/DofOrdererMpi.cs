using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Reordering;
using ISAAR.MSolve.Solvers.Ordering.Reordering;
using MPI;

//TODO: The solver should decide which subdomains will be reused. This class only provides functionality.
namespace ISAAR.MSolve.Solvers.Ordering
{
    /// <summary>
    /// Orders the unconstrained freedom degrees of each subdomain and the shole model. Also applies any reordering and other 
    /// optimizations.
    /// </summary>
    public class DofOrdererMpi //: IDofOrderer
    {
        //TODO: this should also be a strategy, so that I could have caching with fallbacks, in case of insufficient memor.
        private readonly bool cacheElementToSubdomainDofMaps = true;
        private readonly Intracommunicator comm;
        private readonly ConstrainedDofOrderingStrategy constrainedOrderingStrategy;
        private readonly IFreeDofOrderingStrategy freeOrderingStrategy;
        private readonly int masterProcess;
        private readonly int rank;
        private readonly IDofReorderingStrategy reorderingStrategy;

        public DofOrdererMpi(IFreeDofOrderingStrategy freeOrderingStrategy, IDofReorderingStrategy reorderingStrategy, 
            Intracommunicator comm, int masterProcess, bool cacheElementToSubdomainDofMaps = true)
        {
            this.constrainedOrderingStrategy = new ConstrainedDofOrderingStrategy();
            this.freeOrderingStrategy = freeOrderingStrategy;
            this.reorderingStrategy = reorderingStrategy;
            this.cacheElementToSubdomainDofMaps = cacheElementToSubdomainDofMaps;

            this.comm = comm;
            this.rank = comm.Rank;
            this.masterProcess = masterProcess;
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

        public GlobalFreeDofOrderingMpi OrderFreeDofs(IStructuralModel model)
        {
            (int numGlobalFreeDofs, DofTable globalFreeDofs) = freeOrderingStrategy.OrderGlobalDofs(model);
            return new GlobalFreeDofOrderingMpi(numGlobalFreeDofs, globalFreeDofs, comm, masterProcess);
        }

        public ISubdomainFreeDofOrdering OrderFreeDofs(ISubdomain subdomain)
        {
            if (!subdomain.ConnectivityModified) return subdomain.FreeDofOrdering;

            // Order subdomain dofs
            (int numSubdomainFreeDofs, DofTable subdomainFreeDofs) = freeOrderingStrategy.OrderSubdomainDofs(subdomain);
            ISubdomainFreeDofOrdering subdomainOrdering;
            if (cacheElementToSubdomainDofMaps) subdomainOrdering = new SubdomainFreeDofOrderingCaching(
                numSubdomainFreeDofs, subdomainFreeDofs);
            else subdomainOrdering = new SubdomainFreeDofOrderingGeneral(numSubdomainFreeDofs, subdomainFreeDofs);

            // Reorder subdomain dofs
            reorderingStrategy.ReorderDofs(subdomain, subdomainOrdering);

            return subdomainOrdering;
        }
    }
}
