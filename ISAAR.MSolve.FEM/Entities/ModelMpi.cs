using System;
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
        }

        protected override void ScatterSubdomainData()
        {
            // Serialize the data of each subdomain
            Subdomain[] originalSubdomains = null;
            SubdomainDto[] serializedSubdomains = null;
            if (procs.IsMasterProcess)
            {
                int numSubdomains = model.NumSubdomains;
                originalSubdomains = model.SubdomainsDictionary.Values.ToArray();
                serializedSubdomains = new SubdomainDto[procs.Communicator.Size];

                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    if (p == procs.MasterProcess) serializedSubdomains[p] = SubdomainDto.CreateEmpty();
                    else
                    {
                        Subdomain subdomain = model.SubdomainsDictionary[procs.GetSubdomainIdOfProcess(p)];
                        serializedSubdomains[p] = SubdomainDto.Serialize(subdomain, DofSerializer);
                    }
                }
            }

            // Scatter the serialized subdomain data from master process
            SubdomainDto serializedSubdomain = procs.Communicator.Scatter(serializedSubdomains, procs.MasterProcess);

            // Deserialize and store the subdomain data in each process
            if (!procs.IsMasterProcess)
            {
                model = new Model();
                Subdomain subdomain = serializedSubdomain.Deserialize(DofSerializer);
                model.SubdomainsDictionary[subdomain.ID] = subdomain;
                subdomain.ConnectDataStructures();
            }
        }
    }
}
