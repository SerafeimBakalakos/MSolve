using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.FEM.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.XFEM.Elements;
using ISAAR.MSolve.XFEM.Transfer;

//TODO: There is a lot of repetition between this FEM.Model and IGA.Model with regards to interconnection data. That code should 
//      be moved to a common class. Same goes for the interconnection methods of XSubdomain.
namespace ISAAR.MSolve.XFEM.Entities
{
    public class XModelMpi : ModelMpiBase<XModel>
    {
        public XModelMpi(ProcessDistribution processDistribution, Func<XModel> createModel) : base(processDistribution)
        {
            if (processDistribution.IsMasterProcess) this.model = createModel();
        }

        public IDomain2DBoundary Boundary => this.model.Boundary;

        public override void ScatterSubdomains()
        {
            // Serialize the data of each subdomain
            XSubdomain[] originalSubdomains = null;
            XSubdomainDto[] serializedSubdomains = null;
            if (procs.IsMasterProcess)
            {
                int numSubdomains = model.NumSubdomains;
                originalSubdomains = model.Subdomains.Values.ToArray();
                serializedSubdomains = new XSubdomainDto[procs.Communicator.Size];

                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    if (p == procs.MasterProcess) serializedSubdomains[p] = XSubdomainDto.CreateEmpty();
                    else
                    {
                        XSubdomain subdomain = model.Subdomains[procs.GetSubdomainIdOfProcess(p)];
                        serializedSubdomains[p] = XSubdomainDto.Serialize(subdomain, DofSerializer);
                    }
                }
            }

            // Scatter the serialized subdomain data from master process
            XSubdomainDto serializedSubdomain = procs.Communicator.Scatter(serializedSubdomains, procs.MasterProcess);

            // Deserialize and store the subdomain data in each process
            if (!procs.IsMasterProcess)
            {
                model = new XModel();
                XSubdomain subdomain = serializedSubdomain.Deserialize(DofSerializer);
                model.Subdomains[subdomain.ID] = subdomain;
                subdomain.ConnectDataStructures();
            }
        }
    }
}
