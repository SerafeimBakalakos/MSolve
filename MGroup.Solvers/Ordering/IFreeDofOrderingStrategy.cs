﻿using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;

namespace MGroup.Solvers.Ordering
{
    /// <summary>
    /// Determines how the unconstrained freedom degrees of the physical model will be ordered.
    /// Authors: Serafeim Bakalakos
    /// </summary>
    public interface IFreeDofOrderingStrategy
    {
        /// <summary>
        /// Orders the unconstrained freedom degrees of one of the model's subdomains.
        /// </summary>
        /// <param name="subdomain">A subdomain of the whole model.</param>
        (int numSubdomainFreeDofs, DofTable subdomainFreeDofs) OrderSubdomainDofs(ISubdomain subdomain);
    }
}
