using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.XFEM.Elements;
using ISAAR.MSolve.XFEM.Transfer;


//TODO: Transfer level sets and recalculate nodal enrichments in each process. Perhaps also identify which nodes are enriched 
//      with what. 
namespace ISAAR.MSolve.XFEM.Entities
{
    public class XModelMpi : ModelMpiBase<XModel>
    {
        private const int subdomainDataTag = 0;

        //TODO: This does not guarantee that the model also uses the same elementFactory for the elements of this process's 
        //      subdomain.
        private readonly IXFiniteElementFactory elementFactory;

        public XModelMpi(ProcessDistribution processDistribution, Func<XModel> createModel,
            IXFiniteElementFactory elementFactory) : base(processDistribution)
        {
            this.elementFactory = elementFactory;
            if (processDistribution.IsMasterProcess) this.model = createModel();
            else
            {
                this.model = new XModel();
                this.model.Subdomains[procs.OwnSubdomainID] = new XSubdomain(procs.OwnSubdomainID);
            }
        }

        public IDomain2DBoundary Boundary => this.model.Boundary;

        public new IDofSerializer DofSerializer
        {
            set => model.DofSerializer = value;
        }

        public XSubdomain GetXSubdomain(int subdomainID)
        {
            procs.CheckProcessMatchesSubdomainUnlessMaster(subdomainID);
            return model.Subdomains[subdomainID];
        }

        protected override void ScatterSubdomainData()
        {
            HashSet<int> allSubdomainIDs = null;
            if (procs.IsMasterProcess) allSubdomainIDs = new HashSet<int>(model.EnumerateSubdomains().Select(sub => sub.ID));
            ScatterSubdomains(allSubdomainIDs);
        }
        
        public void ScatterSubdomains(HashSet<int> modifiedSubdomains)
        {
            // Broadcast which subdomains are modified
            int[] subdomainIDs = null;
            if (procs.IsMasterProcess) subdomainIDs = modifiedSubdomains.ToArray();
            MpiUtilities.BroadcastArray(procs.Communicator, ref subdomainIDs, procs.MasterProcess);
            if (!procs.IsMasterProcess) modifiedSubdomains = new HashSet<int>(subdomainIDs);

            // Scatter the modified subdomain data
            if (procs.IsMasterProcess)
            {
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    // Serialize and send the data of each subdomain that is modified
                    if (p == procs.MasterProcess) continue;
                    else
                    {
                        foreach (int s in procs.GetSubdomainIdsOfProcess(p))
                        {
                            if (!modifiedSubdomains.Contains(s)) continue;
                            XSubdomain subdomain = model.Subdomains[s];
                            var subdomainDto = XSubdomainDto.Serialize(subdomain, model.DofSerializer);
                            procs.Communicator.Send<XSubdomainDto>(subdomainDto, p, s);
                        }
                    }
                }
            }
            else
            {
                // At first, receive all subdomains of each cluster, so that master process can continue to the next cluster.
                var serializedSubdomains = new Dictionary<int, XSubdomainDto>();
                foreach (int s in procs.GetSubdomainIdsOfProcess(procs.OwnRank))
                {
                    if (!modifiedSubdomains.Contains(s)) continue;
                    serializedSubdomains[s] = procs.Communicator.Receive<XSubdomainDto>(procs.MasterProcess, s);
                }

                // Receive and deserialize and store the subdomain data in processes, where it is modified.
                foreach (int s in serializedSubdomains.Keys)
                {
                    XSubdomain subdomain = model.Subdomains[s];
                    subdomain.ClearEntities();
                    serializedSubdomains[s].Deserialize(subdomain, model.DofSerializer, elementFactory);
                    subdomain.ConnectDataStructures();
                }
            }
        }

        public void ScatterSubdomainsState()
        {
            ScatterSubdomainsState(sub => sub.ConnectivityModified, (sub, modified) => sub.ConnectivityModified = modified);
            ScatterSubdomainsState(sub => sub.StiffnessModified, (sub, modified) => sub.StiffnessModified = modified);
        }

        private void ScatterSubdomainsState(Func<ISubdomain, bool> inquireStateModified, 
            Action<ISubdomain, bool> setStateModified)
        {
            int[] numSubdomainsPerProcess = procs.GetNumSubdomainsPerProcess();
            bool[] areSubdomainsModified = null;
            if (procs.IsMasterProcess)
            {
                areSubdomainsModified = new bool[model.NumSubdomains];
                int offset = 0;
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    foreach (int s in procs.GetSubdomainIdsOfProcess(p))
                    {
                        XSubdomain subdomain = model.Subdomains[s];
                        bool isModified = inquireStateModified(subdomain);
                        areSubdomainsModified[offset++] = isModified;
                    }
                }
            }
            bool[] areProcessSubdomainsModified = procs.Communicator.ScatterFromFlattened(
                areSubdomainsModified, numSubdomainsPerProcess, procs.MasterProcess);
            if (!procs.IsMasterProcess)
            {
                int subOffset = 0; //TODO: This is quite risky: 2 offsets that are processed independently by different processes
                foreach (int s in procs.GetSubdomainIdsOfProcess(procs.OwnRank))
                {
                    XSubdomain subdomain = model.Subdomains[s];
                    bool isModified = areProcessSubdomainsModified[subOffset++];
                    setStateModified(subdomain, isModified);
                }
            }
        }
    }
}
