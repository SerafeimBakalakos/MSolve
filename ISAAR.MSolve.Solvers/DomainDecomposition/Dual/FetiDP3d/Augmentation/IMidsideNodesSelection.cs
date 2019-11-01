using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.Augmentation
{
    public interface IMidsideNodesSelection
    {
        List<INode> MidsideNodesGlobal { get; }

        HashSet<INode> GetMidsideNodesOfSubdomain(ISubdomain subdomain);
    }
}
