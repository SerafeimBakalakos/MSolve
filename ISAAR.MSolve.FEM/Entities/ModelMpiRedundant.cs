using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.Entities;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.FEM.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Distributed;

namespace ISAAR.MSolve.FEM.Entities
{
    public class ModelMpiRedundant : IModelMpi
    {
        private readonly Model model; // This is set in the concrete class's contructor for master and after scattering the subdomains for the other processes.}
        private readonly ProcessDistribution procs;

        public ModelMpiRedundant(ProcessDistribution processDistribution, Func<Model> createModel)
        {
            this.procs = processDistribution;
            this.model = createModel(); // Create the whole model in all processes.
        }

        public Dictionary<int, Cluster> Clusters { get; } = new Dictionary<int, Cluster>();


        public Table<INode, IDofType, double> Constraints => model.Constraints;

        public IDofSerializer DofSerializer => model.DofSerializer;

        public IGlobalFreeDofOrdering GlobalDofOrdering
        {
            get => model.GlobalDofOrdering;
            set => model.GlobalDofOrdering = value;
        }

        public IList<IMassAccelerationHistoryLoad> MassAccelerationHistoryLoads => model.MassAccelerationHistoryLoads;
        public int NumClusters => Clusters.Count;

        public int NumElements => model.NumElements;

        public int NumNodes => model.NumNodes;

        public int NumSubdomains => model.NumSubdomains;

        public void ApplyLoads()
        {
            foreach (int s in procs.GetSubdomainIdsOfProcess(procs.OwnRank))
            {
                ISubdomain subdomain = model.GetSubdomain(s);
                subdomain.Forces.Clear();
            }
        }

        public void ApplyMassAccelerationHistoryLoads(int timeStep) => model.ApplyMassAccelerationHistoryLoads(timeStep);

        public void ConnectDataStructures()
        {
            model.ConnectDataStructures();
            for (int p = 0; p < procs.Communicator.Size; ++p)
            {
                var cluster = new Cluster(p);
                foreach (int s in procs.GetSubdomainIdsOfProcess(p)) cluster.Subdomains.Add(model.GetSubdomain(s));
                Clusters[p] = cluster;
            }
        }

        public IEnumerable<Cluster> EnumerateClusters() => Clusters.Values;

        public IEnumerable<IElement> EnumerateElements() => model.EnumerateElements();

        public IEnumerable<INode> EnumerateNodes() => model.EnumerateNodes();

        public IEnumerable<ISubdomain> EnumerateSubdomains() => model.EnumerateSubdomains();

        public Cluster GetCluster(int clusterID) => Clusters[clusterID];

        public IElement GetElement(int elementID) => model.GetElement(elementID);

        public INode GetNode(int nodeID) => model.GetNode(nodeID);

        public ISubdomain GetSubdomain(int subdomainID) => model.GetSubdomain(subdomainID);

        public void ScatterSubdomains()
        {
            // Do nothing
        }
    }
}
