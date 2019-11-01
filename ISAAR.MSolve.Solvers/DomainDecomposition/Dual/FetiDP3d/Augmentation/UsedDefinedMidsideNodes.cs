using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.Augmentation
{
    public class UsedDefinedMidsideNodes : IMidsideNodesSelection
    {
        private readonly Dictionary<ISubdomain, HashSet<INode>> midsideNodesOfSubdomains;
        private readonly List<INode> midsideNodesGlobal;

        public UsedDefinedMidsideNodes(Dictionary<ISubdomain, HashSet<INode>> midsideNodesOfSubdomains)
        {
            this.midsideNodesOfSubdomains = midsideNodesOfSubdomains;
            var globalNodes = new SortedSet<INode>(); // I sort them only to match the order of Qr columns in the tests. 
            foreach (IEnumerable<INode> subdomainNodes in midsideNodesOfSubdomains.Values)
            {
                globalNodes.UnionWith(subdomainNodes);
            }
            midsideNodesGlobal = globalNodes.ToList();
        }

        public List<INode> MidsideNodesGlobal => midsideNodesGlobal;

        public HashSet<INode> GetMidsideNodesOfSubdomain(ISubdomain subdomain) => midsideNodesOfSubdomains[subdomain];

        public void Update() { }
    }
}
