﻿using System;
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

        public XSubdomain GetXSubdomain(int subdomainID)
        {
            procs.CheckProcessMatchesSubdomainUnlessMaster(subdomainID);
            return model.Subdomains[subdomainID];
        }
        public override void ScatterSubdomains()
        {
            BroadcastSubdomainsState();

            if (procs.IsMasterProcess)
            {
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    // Serialize and send the data of each subdomain that is modified
                    if (p == procs.MasterProcess) continue;
                    else
                    {
                        XSubdomain subdomain = model.Subdomains[procs.GetSubdomainIdOfProcess(p)];
                        if (subdomain.ConnectivityModified)
                        {
                            XSubdomainDto subdomainDto = XSubdomainDto.Serialize(subdomain, DofSerializer);
                            procs.Communicator.Send<XSubdomainDto>(subdomainDto, p, subdomainDataTag);
                        }
                    }
                }
            }
            else
            {
                // Receive and deserialize and store the subdomain data in processes, where it is modified.
                XSubdomain subdomain = model.Subdomains[procs.OwnSubdomainID];
                if (subdomain.ConnectivityModified)
                {
                    subdomain.ClearEntities();
                    XSubdomainDto serializedSubdomain = 
                        procs.Communicator.Receive<XSubdomainDto>(procs.MasterProcess, subdomainDataTag);
                    serializedSubdomain.Deserialize(subdomain, DofSerializer, elementFactory);
                    subdomain.ConnectDataStructures();
                }
            }
        }

        //public override void ScatterSubdomains()
        //{
        //    BroadcastSubdomainsState();

        //    // Serialize the data of each subdomain
        //    XSubdomainDto[] serializedSubdomains = null;
        //    if (procs.IsMasterProcess)
        //    {
        //        serializedSubdomains = new XSubdomainDto[procs.Communicator.Size];

        //        for (int p = 0; p < procs.Communicator.Size; ++p)
        //        {
        //            if (p == procs.MasterProcess) serializedSubdomains[p] = XSubdomainDto.CreateEmpty();
        //            else
        //            {
        //                XSubdomain subdomain = model.Subdomains[procs.GetSubdomainIdOfProcess(p)];
        //                serializedSubdomains[p] = XSubdomainDto.Serialize(subdomain, DofSerializer);
        //            }
        //        }
        //    }

        //    // Scatter the serialized subdomain data from master process
        //    XSubdomainDto serializedSubdomain = procs.Communicator.Scatter(serializedSubdomains, procs.MasterProcess);

        //    // Deserialize and store the subdomain data in each process
        //    if (!procs.IsMasterProcess)
        //    {
        //        XSubdomain subdomain = model.Subdomains[procs.OwnSubdomainID];
        //        serializedSubdomain.Deserialize(subdomain, DofSerializer, elementFactory);
        //        subdomain.ConnectDataStructures();
        //    }
        //}

        private void BroadcastSubdomainsState()
        {
            ISubdomain ownSubdomain = GetSubdomain(procs.OwnSubdomainID);

            // Connectivity
            bool[] areSubdomainsModified = null;
            if (procs.IsMasterProcess)
            {
                areSubdomainsModified = new bool[procs.Communicator.Size];
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    XSubdomain subdomain = model.Subdomains[procs.GetSubdomainIdOfProcess(p)];
                    areSubdomainsModified[ p] = subdomain.ConnectivityModified;
                }
            }
            ownSubdomain.ConnectivityModified = procs.Communicator.Scatter<bool>(areSubdomainsModified, procs.MasterProcess);

            // Stiffness
            if (procs.IsMasterProcess)
            {
                areSubdomainsModified = new bool[procs.Communicator.Size];
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    XSubdomain subdomain = model.Subdomains[procs.GetSubdomainIdOfProcess(p)];
                    areSubdomainsModified[p] = subdomain.StiffnessModified;
                }
            }
            ownSubdomain.StiffnessModified = procs.Communicator.Scatter<bool>(areSubdomainsModified, procs.MasterProcess);
        }
    }
}
