using System;
using System.Collections.Generic;
using System.Text;
using MGroup.Solvers.Distributed.Environments;
using MGroup.Solvers.Distributed.LinearAlgebra;
using MGroup.Solvers.Distributed.Topologies;

// Global:
// 0--1--2--3--4--5--6--7--8
//
// Clusters:
// 0--1--2  c0
//       2--3--4  c1
//             4--5--6  c2
//                   6--7--8  c3
//
// Subdomains:
// 0--1  s0
//    1--2  s1
//       2--3  s2
//          3--4  s3
//             4--5  s4
//                5--6  s5
//                   6--7  s6
//                      7--8  s7
//
namespace MGroup.Solvers.Tests.Distributed.LinearAlgebra
{
    public class Line1DTopology
    {
        public ComputeNodeTopology CreateNodeTopology()
        {
            // Compute nodes
            var topology = new ComputeNodeTopology();
            topology.Nodes[0] = new ComputeNode(0);
            topology.Nodes[1] = new ComputeNode(1);
            topology.Nodes[2] = new ComputeNode(2);
            topology.Nodes[3] = new ComputeNode(3);

            // Neighborhoods
            topology.Nodes[0].Neighbors.Add(topology.Nodes[1]);
            topology.Nodes[1].Neighbors.Add(topology.Nodes[0]);
            topology.Nodes[1].Neighbors.Add(topology.Nodes[2]);
            topology.Nodes[2].Neighbors.Add(topology.Nodes[1]);
            topology.Nodes[2].Neighbors.Add(topology.Nodes[3]);
            topology.Nodes[3].Neighbors.Add(topology.Nodes[2]);

            return topology;
        }

        public Dictionary<ComputeNode, DistributedIndexer> CreateIndexers(IComputeEnvironment environment,
            ComputeNodeTopology topology)
        {
            var indexers = new Dictionary<ComputeNode, DistributedIndexer>();

            var indexer0 = new DistributedIndexer(topology.Nodes[0]);
            var commonEntries0 = new Dictionary<ComputeNode, int[]>();
            commonEntries0[topology.Nodes[1]] = new int[] { 2 };
            indexer0.Initialize(3, commonEntries0);
            indexers[indexer0.Node] = indexer0;

            var indexer1 = new DistributedIndexer(topology.Nodes[1]);
            var commonEntries1 = new Dictionary<ComputeNode, int[]>();
            commonEntries1[topology.Nodes[0]] = new int[] { 0 };
            commonEntries1[topology.Nodes[2]] = new int[] { 2 };
            indexer1.Initialize(3, commonEntries1);
            indexers[indexer1.Node] = indexer1;

            var indexer2 = new DistributedIndexer(topology.Nodes[2]);
            var commonEntries2 = new Dictionary<ComputeNode, int[]>();
            commonEntries2[topology.Nodes[1]] = new int[] { 0 };
            commonEntries2[topology.Nodes[3]] = new int[] { 2 };
            indexer2.Initialize(3, commonEntries2);
            indexers[indexer2.Node] = indexer2;

            var indexer3 = new DistributedIndexer(topology.Nodes[3]);
            var commonEntries3 = new Dictionary<ComputeNode, int[]>();
            commonEntries3[topology.Nodes[2]] = new int[] { 0 };
            indexer3.Initialize(3, commonEntries3);
            indexers[indexer3.Node] = indexer3;

            Utilities.FilterNodeData(environment, indexers);
            return indexers;
        }

        public Dictionary<ComputeNode, int[]> CreateLocalToGlobalMaps(IComputeEnvironment environment,
            ComputeNodeTopology topology)
        {
            var localToGlobalMaps = new Dictionary<ComputeNode, int[]>();
            localToGlobalMaps[topology.Nodes[0]] = new int[] { 0, 1, 2 };
            localToGlobalMaps[topology.Nodes[1]] = new int[] { 2, 3, 4 };
            localToGlobalMaps[topology.Nodes[2]] = new int[] { 4, 5, 6 };
            localToGlobalMaps[topology.Nodes[3]] = new int[] { 6, 7, 8 };

            Utilities.FilterNodeData(environment, localToGlobalMaps);
            return localToGlobalMaps;
        }
    }
}
