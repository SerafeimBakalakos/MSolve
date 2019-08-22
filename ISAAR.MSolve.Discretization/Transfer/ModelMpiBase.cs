using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.Exceptions;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.FEM.Interfaces;

//TODO: The redirection to IModel is necessary to safeguard against a process trying to access unavailable data, but it slows 
//      access to the items needed by client code. This is especially pronounced for GetNode(), GetElement(), etc that will be 
//      called a lot of times in succession. Can it be optimized?
//TODO: Can the boilerplate code be reduced without sacrificing performance?
//TODO: Isn't it wasteful to create all this indirection, when the only things actually implemented by this are IDofSerializer, 
//      ScatterSubdomains() and GetSubdomain()? Must I use polymorphism for them? On the other hand, once updating the model is
//      is considered, it will be nice to let ModelMpi handle the communication before and after updating model fields.
namespace ISAAR.MSolve.Discretization.Transfer
{
    public abstract class ModelMpiBase<TModel> : IModelMpi
        where TModel : IModel
    {
        protected readonly ProcessDistribution procs;
        protected TModel model; // This is set in the concrete class's contructor for master and after scattering the subdomains for the other processes.

        protected ModelMpiBase(ProcessDistribution processDistribution)
        {
            this.procs = processDistribution;
        }

        public Table<INode, IDofType, double> Constraints
        {
            get
            {
                if (procs.IsMasterProcess) return model.Constraints;
                else throw StandardProcessException;
            }
        }

        public IDofSerializer DofSerializer => model.DofSerializer;

        public IGlobalFreeDofOrdering GlobalDofOrdering
        {
            get => model.GlobalDofOrdering;
            set => model.GlobalDofOrdering = value;
        }

        public IList<IMassAccelerationHistoryLoad> MassAccelerationHistoryLoads
        {
            get
            {
                if (procs.IsMasterProcess) return model.MassAccelerationHistoryLoads;
                else throw StandardProcessException;
            }
        }

        public int NumElements
        {
            get
            {
                if (procs.IsMasterProcess) return model.NumElements;
                else throw StandardProcessException;
            }
        }

        public int NumNodes
        {
            get
            {
                if (procs.IsMasterProcess) return model.NumNodes;
                else throw StandardProcessException;
            }
        }

        public int NumSubdomains
        {
            get
            {
                if (procs.IsMasterProcess) return model.NumSubdomains;
                else throw StandardProcessException;
            }
        }

        private MpiException StandardProcessException
            => new MpiException($"Process {procs.OwnRank}: Only defined for master process (rank = {procs.MasterProcess})");


        public void AssignLoads(NodalLoadsToSubdomainsDistributor distributeNodalLoads)
        {
            if (procs.IsMasterProcess) model.AssignLoads(distributeNodalLoads);
            else throw StandardProcessException;
        }

        public void AssignMassAccelerationHistoryLoads(int timeStep)
        {
            if (procs.IsMasterProcess) model.AssignMassAccelerationHistoryLoads(timeStep);
            else throw StandardProcessException;
        }

        public void ConnectDataStructures()
        {
            if (procs.IsMasterProcess) model.ConnectDataStructures();
            // If it is not master, then just return
        }

        public IEnumerable<IElement> EnumerateElements()
        {
            if (procs.IsMasterProcess) return model.EnumerateElements();
            else throw StandardProcessException;
        }

        public IEnumerable<INode> EnumerateNodes()
        {
            if (procs.IsMasterProcess) return model.EnumerateNodes();
            else throw StandardProcessException;
        }

        public IEnumerable<ISubdomain> EnumerateSubdomains()
        {
            if (procs.IsMasterProcess) return model.EnumerateSubdomains();
            else throw StandardProcessException;
        }

        public IElement GetElement(int elementID)
        {
            if (procs.IsMasterProcess) return model.GetElement(elementID);
            else throw StandardProcessException;
        }

        public INode GetNode(int nodeID)
        {
            if (procs.IsMasterProcess) return model.GetNode(nodeID);
            else throw StandardProcessException;
        }

        public ISubdomain GetSubdomain(int subdomainID) => model.GetSubdomain(subdomainID);

        public abstract void ScatterSubdomains();
    }
}
