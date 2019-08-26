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
                MpiException.CheckProcessIsMaster(procs);
                return model.Constraints;
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
                MpiException.CheckProcessIsMaster(procs);
                return model.MassAccelerationHistoryLoads;
            }
        }

        public int NumElements
        {
            get
            {
                MpiException.CheckProcessIsMaster(procs);
                return model.NumElements;
            }
        }

        public int NumNodes
        {
            get
            {
                MpiException.CheckProcessIsMaster(procs);
                return model.NumNodes;
            }
        }

        public int NumSubdomains
        {
            get
            {
                MpiException.CheckProcessIsMaster(procs);
                return model.NumSubdomains;
            }
        }

        public void ApplyLoads()
        {
            MpiException.CheckProcessIsMaster(procs);
            model.ApplyLoads();
        }

        public void ApplyMassAccelerationHistoryLoads(int timeStep)
        {
            MpiException.CheckProcessIsMaster(procs);
            model.ApplyMassAccelerationHistoryLoads(timeStep);
        }

        public void ConnectDataStructures()
        {
            if (procs.IsMasterProcess) model.ConnectDataStructures();
            // If it is not master, then just return
        }

        public IEnumerable<IElement> EnumerateElements()
        {
            MpiException.CheckProcessIsMaster(procs);
            return model.EnumerateElements();
        }

        public IEnumerable<INode> EnumerateNodes()
        {
            MpiException.CheckProcessIsMaster(procs);
            return model.EnumerateNodes();
        }

        public IEnumerable<ISubdomain> EnumerateSubdomains()
        {
            MpiException.CheckProcessIsMaster(procs);
            return model.EnumerateSubdomains();
        }

        public IElement GetElement(int elementID)
        {
            MpiException.CheckProcessIsMaster(procs);
            return model.GetElement(elementID);
        }

        public INode GetNode(int nodeID)
        {
            MpiException.CheckProcessIsMaster(procs);
            return model.GetNode(nodeID);
        }

        public ISubdomain GetSubdomain(int subdomainID) => model.GetSubdomain(subdomainID);

        public abstract void ScatterSubdomains();
    }
}
