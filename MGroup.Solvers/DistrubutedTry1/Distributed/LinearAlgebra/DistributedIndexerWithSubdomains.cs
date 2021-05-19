using System;
using System.Collections.Generic;
using System.Text;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.Environments;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.Topologies;

namespace MGroup.Solvers_OLD.DistributedTry1.Distributed.LinearAlgebra
{
    public class DistributedIndexerWithSubdomains
    {
        private readonly Dictionary<int, int[]> subdomainToClusterEntries;


        public DistributedIndexerWithSubdomains(IComputeEnvironment environment)
        {
            subdomainToClusterEntries = environment.CreateDictionaryPerSubnode<int[]>(s => null);
            ClusterIndexer = new DistributedIndexer(environment.NodeTopology.Nodes.Values);
        }

        public DistributedIndexer ClusterIndexer { get; }

        public void ConfigureForSubnode(ComputeSubnode subnode, int[] subdomainToClusterMap)
            => subdomainToClusterEntries[subnode.ID] = subdomainToClusterMap;

        public int GetNumEntries(ComputeSubnode subnode) => subdomainToClusterEntries[subnode.ID].Length;

        public int[] MapSubdomainToClusterEntries(ComputeSubnode subnode) => subdomainToClusterEntries[subnode.ID];
    }
}
