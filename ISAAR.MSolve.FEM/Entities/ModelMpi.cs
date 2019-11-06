using System;
using System.Collections.Generic;
using System.Linq;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.FEM.Transfer;

namespace ISAAR.MSolve.FEM.Entities
{
    public class ModelMpi : ModelMpiBase<Model>
    {
        public ModelMpi(ProcessDistribution processDistribution, Func<Model> createModel) : 
            base(processDistribution)
        {
            if (processDistribution.IsMasterProcess) this.model = createModel();
            else this.model = new Model();
        }

        protected override void ScatterSubdomainData()
        {
            // Serialize the data of each subdomain, one cluster at a time
            if (procs.IsMasterProcess)
            {
                int numSubdomains = model.NumSubdomains;
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    if (p == procs.MasterProcess) continue;
                    else
                    {
                        foreach (int s in procs.GetSubdomainIdsOfProcess(p))
                        {
                            Subdomain subdomain = model.SubdomainsDictionary[s];
                            var subdomainDto = SubdomainDto.Serialize(subdomain, DofSerializer);
                            procs.Communicator.Send<SubdomainDto>(subdomainDto, p, s);
                        }
                    }
                }
            }
            else
            {
                // At first, receive all subdomains of each cluster, so that master process can continue to the next cluster.
                var serializedSubdomains = new Dictionary<int, SubdomainDto>();
                foreach (int s in procs.GetSubdomainIdsOfProcess(procs.OwnRank))
                {
                    serializedSubdomains[s] = procs.Communicator.Receive<SubdomainDto>(procs.MasterProcess, s);
                }

                // Deserialize and store the subdomain data in each process
                foreach (int s in procs.GetSubdomainIdsOfProcess(procs.OwnRank))
                {
                    Subdomain subdomain = serializedSubdomains[s].Deserialize(DofSerializer);
                    model.SubdomainsDictionary[subdomain.ID] = subdomain;
                    subdomain.ConnectDataStructures();
                }
            }
        }
    }
}
