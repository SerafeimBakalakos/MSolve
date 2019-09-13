using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.CornerNodes;
using ISAAR.MSolve.XFEM.CrackGeometry;
using ISAAR.MSolve.XFEM.Elements;
using ISAAR.MSolve.XFEM.Entities;

//TODO: It is possible that some previous corner nodes become internal due to the TipAdaptivePartitioner. How to handle this?
namespace ISAAR.MSolve.XFEM.Solvers
{
    public class CrackedFetiDPCornerNodesSerial : ICornerNodeSelection
    {
        private readonly ICrackDescription crack;
        //private readonly Dictionary<int, HashSet<INode>> currentCornerNodes;
        private HashSet<INode> cornerNodesGlobal;
        private Dictionary<ISubdomain, HashSet<INode>> cornerNodesOfSubdomains;

        public CrackedFetiDPCornerNodesSerial(ICrackDescription crack,
           Dictionary<ISubdomain, HashSet<INode>> initialCornerNodes)
        {
            this.crack = crack;
            this.cornerNodesOfSubdomains = initialCornerNodes;

            // Gather global corner nodes
            cornerNodesGlobal = new HashSet<INode>();
            foreach (IEnumerable<INode> subdomainNodes in cornerNodesOfSubdomains.Values)
            {
                cornerNodesGlobal.UnionWith(subdomainNodes);
            }
        }

        public HashSet<INode> GlobalCornerNodes => cornerNodesGlobal;

        public HashSet<INode> GetCornerNodesOfSubdomain(ISubdomain subdomain) => cornerNodesOfSubdomains[subdomain];

        public Dictionary<ISubdomain, HashSet<INode>> SelectCornerNodesOfSubdomains()
        {
            // Remove the previous corner nodes that are no longer boundary.
            foreach (HashSet<INode> subdomainCorners in cornerNodesOfSubdomains.Values)
            {
                subdomainCorners.RemoveWhere(node => node.Multiplicity < 2);
            }

            // Add boundary Heaviside nodes and nodes of the tip element(s).
            HashSet<XNode> enrichedBoundaryNodes = FindNewEnrichedBoundaryNodes();
            foreach (XNode node in enrichedBoundaryNodes)
            {
                foreach (ISubdomain subdomain in node.SubdomainsDictionary.Values)
                {
                    cornerNodesOfSubdomains[subdomain].Add(node);
                }
            }
            return cornerNodesOfSubdomains;
        }

        public void Update() //TODO: This does not need to be called the first time.
        {
            // Remove the previous corner nodes that are no longer boundary.
            foreach (HashSet<INode> subdomainCorners in cornerNodesOfSubdomains.Values)
            {
                subdomainCorners.RemoveWhere(node => node.Multiplicity < 2);
            }

            // Add boundary Heaviside nodes and nodes of the tip element(s).
            HashSet<XNode> enrichedBoundaryNodes = FindNewEnrichedBoundaryNodes();
            foreach (XNode node in enrichedBoundaryNodes)
            {
                foreach (ISubdomain subdomain in node.SubdomainsDictionary.Values)
                {
                    cornerNodesOfSubdomains[subdomain].Add(node);
                }
            }

            // Gather global corner nodes
            cornerNodesGlobal = new HashSet<INode>();
            foreach (IEnumerable<INode> subdomainNodes in cornerNodesOfSubdomains.Values)
            {
                cornerNodesGlobal.UnionWith(subdomainNodes);
            }
        }

        private HashSet<XNode> FindNewEnrichedBoundaryNodes()
        {
            var enrichedBoundaryNodes = new HashSet<XNode>();
            foreach (CartesianPoint crackTip in crack.CrackTips)
            {
                foreach (XContinuumElement2D tipElement in crack.CrackTipElements[crackTip])
                {
                    foreach (XNode node in tipElement.Nodes)
                    {
                        if (node.Multiplicity > 1) enrichedBoundaryNodes.Add(node);
                    }
                }
            }
            foreach (var crackBodyNewNodes in crack.CrackBodyNodesNew)
            {
                foreach (XNode node in crackBodyNewNodes.Value)
                {
                    if (node.Multiplicity > 1) enrichedBoundaryNodes.Add(node);
                }
            }
            return enrichedBoundaryNodes;
        }
    }
}
