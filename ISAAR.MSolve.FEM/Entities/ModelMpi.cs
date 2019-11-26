using System;
using System.Collections.Generic;
using System.Linq;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.FEM.Transfer;
using ISAAR.MSolve.LinearAlgebra.Distributed;
using ISAAR.MSolve.LinearAlgebra.Distributed.Transfer;

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
            // Scatter subdomain data to all processes
            var transferrer = new TransferrerPerSubdomain(procs);
            PackSubdomainData<Subdomain, SubdomainDto> packData = 
                (id, subdomain) => SubdomainDto.Serialize(subdomain, DofSerializer);
            UnpackSubdomainData<Subdomain, SubdomainDto> unpackData =
                (id, subdomainDto) => subdomainDto.Deserialize(DofSerializer);
            Dictionary<int, Subdomain> subdomainsOfProcess = transferrer.ScatterToAllSubdomainsPacked(
                model.SubdomainsDictionary, packData, unpackData);

            if (!procs.IsMasterProcess)
            {
                // Add the subdomains to the model
                foreach (Subdomain subdomain in subdomainsOfProcess.Values)
                {
                    model.SubdomainsDictionary[subdomain.ID] = subdomain;
                    subdomain.ConnectDataStructures();
                }
            }
        }
    }
}
