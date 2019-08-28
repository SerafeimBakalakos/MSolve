using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Solvers.Ordering.Reordering;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation
{
    public interface IFetiDPSeparatedDofReordering
    {
        DofPermutation ReorderGlobalCornerDofs(IFetiDPDofSeparator dofSeparator);
        DofPermutation ReorderSubdomainInternalDofs(ISubdomain subdomain, IFetiDPDofSeparator dofSeparator);
        DofPermutation ReorderSubdomainRemainderDofs(ISubdomain subdomain, IFetiDPDofSeparator dofSeparator);
    }
}