using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.Augmentation
{
    public class UsedDefinedMidsideNodes : IMidsideNodesSelection
    {
        private readonly Dictionary<ISubdomain, HashSet<INode>> midsideNodesOfSubdomains;
        private readonly HashSet<INode> midsideNodesGlobal;

        public UsedDefinedMidsideNodes(Dictionary<ISubdomain, HashSet<INode>> midsideNodesOfSubdomains)
        {
            this.midsideNodesOfSubdomains = midsideNodesOfSubdomains;
            this.midsideNodesGlobal = new HashSet<INode>();
            foreach (IEnumerable<INode> subdomainNodes in midsideNodesOfSubdomains.Values)
            {
                midsideNodesGlobal.UnionWith(subdomainNodes);
            }
        }

        public HashSet<INode> MidsideNodesGlobal => midsideNodesGlobal;

        public HashSet<INode> GetMidsideNodesOfSubdomain(ISubdomain subdomain) => midsideNodesOfSubdomains[subdomain];

        public void Update() { }
    }
}
