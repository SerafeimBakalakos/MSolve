﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.CornerNodes;
using ISAAR.MSolve.XFEM.CrackGeometry;
using ISAAR.MSolve.XFEM.Elements;
using ISAAR.MSolve.XFEM.Entities;

//TODO: Remove duplication between this and the serial implementation.
//TODO: It is possible that some previous corner nodes become internal due to the TipAdaptivePartitioner. How to handle this?
namespace ISAAR.MSolve.XFEM.Solvers
{
    public class CrackedFetiDPCornerNodesMpi : ICornerNodeSelection
    {
        private const int cornerNodesTag = 0;

        private readonly ICrackDescription crack;
        private readonly Func<ISubdomain, HashSet<INode>> getInitialCornerNodes;
        private readonly IModel model;
        private readonly ProcessDistribution procs;

        private Dictionary<ISubdomain, bool> areCornerNodesModified;
        private bool areGlobalCornerNodesModified_master = true;
        private HashSet<INode> cornerNodesGlobal_master;
        private Dictionary<ISubdomain, HashSet<INode>> cornerNodesOfSubdomains = new Dictionary<ISubdomain, HashSet<INode>>();
        private bool isFirstAnalysis; //TODO: This should be passed to Update(). Update should not be called by the solver.

        public CrackedFetiDPCornerNodesMpi(ProcessDistribution processDistribution, IModel model, ICrackDescription crack,
           Func<ISubdomain, HashSet<INode>> getInitialCornerNodes)
        {
            this.procs = processDistribution;
            this.model = model;
            this.crack = crack;
            this.getInitialCornerNodes = getInitialCornerNodes;
            this.isFirstAnalysis = true;
        }

        public bool AreGlobalCornerNodesModified
        {
            get
            {
                procs.CheckProcessIsMaster();
                return areGlobalCornerNodesModified_master;
            }
        }

        public HashSet<INode> GlobalCornerNodes
        {
            get
            {
                procs.CheckProcessIsMaster();
                return cornerNodesGlobal_master;
            }
        }

        public bool AreCornerNodesOfSubdomainModified(ISubdomain subdomain)
        {
            procs.CheckProcessMatchesSubdomainUnlessMaster(subdomain.ID);
            return areCornerNodesModified[subdomain];
        }

        public HashSet<INode> GetCornerNodesOfSubdomain(ISubdomain subdomain)
        {
            procs.CheckProcessMatchesSubdomainUnlessMaster(subdomain.ID);
            return cornerNodesOfSubdomains[subdomain];
        }

        public void Update()
        {
            if (procs.IsMasterProcess)
            {
                if (isFirstAnalysis)
                {
                    foreach (ISubdomain subdomain in model.EnumerateSubdomains())
                    {
                        cornerNodesOfSubdomains[subdomain] = getInitialCornerNodes(subdomain);
                    }
                }

                // Keep track of subdomains with modified corner nodes.
                areCornerNodesModified = new Dictionary<ISubdomain, bool>();
                foreach (ISubdomain subdomain in cornerNodesOfSubdomains.Keys)
                {
                    areCornerNodesModified[subdomain] = isFirstAnalysis;
                }

                // Remove the previous corner nodes that are no longer boundary.
                foreach (ISubdomain subdomain in cornerNodesOfSubdomains.Keys)
                {
                    int numEntriesRemoved = cornerNodesOfSubdomains[subdomain].RemoveWhere(node => node.Multiplicity < 2);
                    if (numEntriesRemoved > 0) areCornerNodesModified[subdomain] = true;
                }

                // Remove the previous corner nodes that do not belong to each subdomain
                foreach (ISubdomain subdomain in cornerNodesOfSubdomains.Keys)
                {
                    var removedNodes = new HashSet<INode>();
                    foreach (INode node in cornerNodesOfSubdomains[subdomain])
                    {
                        try
                        {
                            subdomain.GetNode(node.ID);
                        }
                        catch (KeyNotFoundException)
                        {
                            removedNodes.Add(node);
                        }
                    }
                    foreach (INode node in removedNodes) cornerNodesOfSubdomains[subdomain].Remove(node);
                    if (removedNodes.Count > 0) areCornerNodesModified[subdomain] = true;
                }

                // Add boundary Heaviside nodes and nodes of the tip element(s).
                HashSet<XNode> enrichedBoundaryNodes = FindNewEnrichedBoundaryNodes();
                foreach (XNode node in enrichedBoundaryNodes)
                {
                    foreach (ISubdomain subdomain in node.SubdomainsDictionary.Values)
                    {
                        cornerNodesOfSubdomains[subdomain].Add(node);
                        areCornerNodesModified[subdomain] = true;
                    }
                }

                GatherGlobalCornerNodes_master();
            }
            ScatterSubdomainCornerNodes();
            isFirstAnalysis = false;
            //WriteCornerNodes();
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

        private void GatherGlobalCornerNodes_master()
        {
            // Define them
            cornerNodesGlobal_master = new HashSet<INode>();
            foreach (IEnumerable<INode> subdomainNodes in cornerNodesOfSubdomains.Values)
            {
                cornerNodesGlobal_master.UnionWith(subdomainNodes);
            }

            // Determine if they are modified
            areGlobalCornerNodesModified_master = false;
            foreach (bool modifiedSubdomain in areCornerNodesModified.Values)
            {
                if (modifiedSubdomain)
                {
                    areGlobalCornerNodesModified_master = true;
                    break;
                }
            }
        }

        private void ScatterSubdomainCornerNodes()
        {


            if (procs.IsMasterProcess)
            {
                // Send to each process the corner nodes of its subdomain, if they are modified.
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    if (p == procs.MasterProcess) continue;
                    ISubdomain subdomain = model.GetSubdomain(procs.GetSubdomainIdOfProcess(p));
                    if (subdomain.ConnectivityModified)
                    {
                        HashSet<INode> cornerNodes = cornerNodesOfSubdomains[subdomain];
                        int[] cornerIDs = cornerNodes.Select(n => n.ID).ToArray();
                        MpiUtilities.SendArray<int>(procs.Communicator, cornerIDs, p, cornerNodesTag);
                    }
                }
            }
            else
            {
                ISubdomain subdomain = model.GetSubdomain(procs.OwnSubdomainID);
                // Receive the corner nodes from master, if they are modified.
                if (subdomain.ConnectivityModified)
                {
                    int[] cornerIDs = MpiUtilities.ReceiveArray<int>(procs.Communicator, procs.MasterProcess, cornerNodesTag);
                    var cornerNodes = new HashSet<INode>();
                    foreach (int n in cornerIDs) cornerNodes.Add(subdomain.GetNode(n));
                    cornerNodesOfSubdomains[subdomain] = cornerNodes;
                }
            }
        }

        private void WriteCornerNodes()
        {
            MpiUtilities.DoInTurn(procs.Communicator, () =>
            {
                int s = procs.OwnSubdomainID;
                Console.Write($"Process {procs.OwnRank}: Corner nodes of subdomain {s}: ");
                foreach (INode node in cornerNodesOfSubdomains[model.GetSubdomain(s)]) Console.Write(node.ID + " ");
                Console.WriteLine();
            });
        }
    }
}