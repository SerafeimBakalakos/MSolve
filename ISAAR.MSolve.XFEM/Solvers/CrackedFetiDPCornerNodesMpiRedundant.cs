using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Distributed;
using ISAAR.MSolve.XFEM.CrackGeometry;

//TODO: Remove duplication between this and the serial implementation.
//TODO: It is possible that some previous corner nodes become internal due to the TipAdaptivePartitioner. How to handle this?
namespace ISAAR.MSolve.XFEM.Solvers
{
    /// <summary>
    /// Assumes all model data are stored in all processes. Each process will only deal with the corner nodes of its 
    /// corresponding subdomains. Master process will deal with all suubdomain and global data.
    /// </summary>
    public class CrackedFetiDPCornerNodesMpiRedundant: CrackedFetiDPCornerNodesBase
    {
        private const int cornerNodesTag = 0;

        private readonly IModel model;
        private readonly ProcessDistribution procs;

        public CrackedFetiDPCornerNodesMpiRedundant(ProcessDistribution processDistribution, IModel model, 
            ICrackDescription crack, Func<ISubdomain, HashSet<INode>> getInitialCornerNodes) :
            base(crack, getInitialCornerNodes)
        {
            this.procs = processDistribution;
            this.model = model;
        }

        public bool AreGlobalCornerNodesModified
        {
            get
            {
                procs.CheckProcessIsMaster();
                return areGlobalCornerNodesModified;
            }
        }

        public override HashSet<INode> GlobalCornerNodes
        {
            get
            {
                procs.CheckProcessIsMaster();
                return cornerNodesGlobal;
            }
        }

        public bool AreCornerNodesOfSubdomainModified(ISubdomain subdomain)
        {
            procs.CheckProcessMatchesSubdomainUnlessMaster(subdomain.ID);
            return areCornerNodesModified[subdomain];
        }

        public override HashSet<INode> GetCornerNodesOfSubdomain(ISubdomain subdomain)
        {
            procs.CheckProcessMatchesSubdomainUnlessMaster(subdomain.ID);
            return cornerNodesOfSubdomains[subdomain];
        }

        public override void Update()
        {
            if (procs.IsMasterProcess)
            {
                base.UpdateSubdomainsCorners(model.EnumerateSubdomains());
                base.GatherGlobalCornerNodes();
            }
            else
            {
                int[] subdomainIDs = procs.GetSubdomainIdsOfProcess(procs.OwnRank);
                IEnumerable<ISubdomain> subdomainsToUpdate = subdomainIDs.Select(s => model.GetSubdomain(s));
                base.UpdateSubdomainsCorners(subdomainsToUpdate);
            }
            isFirstAnalysis = false;
            //WriteCornerNodes();
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
