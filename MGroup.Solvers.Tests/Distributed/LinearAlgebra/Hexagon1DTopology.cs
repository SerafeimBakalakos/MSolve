using System;
using System.Collections.Generic;
using System.Text;
using MGroup.Solvers.Distributed.Environments;
using MGroup.Solvers.Distributed.LinearAlgebra;
using MGroup.Solvers.Distributed.Topologies;

//          x 8                   x 8                     x 8      
//        /   \                 /                           \      
//    9  x     x 7          9  x                             x 7      
//     /         \           /                                 \      
// 10 x           x 6    10 x    cluster2                       x 6  cluster1      
//    |           |         |                                   |      
// 11 x           x 5    11 x                                   x 5       
//    |           |         |                                   |      
//  0 x           x 4     0 x       0 x           x 4         x 4       
//     \         /                     \         /
//     1 x     x 3                     1 x     x 3
//        \   /                           \   /
//        2 x                             2 x   
//                                       cluster0
// 1 dof per node

namespace MGroup.Solvers.Tests.Distributed.LinearAlgebra
{
    public class Hexagon1DTopology
    {
        public ComputeNodeTopology CreateNodeTopology()
        {
            // Compute nodes
            var topology = new ComputeNodeTopology();
            topology.Nodes[0] = new ComputeNode(0);
            topology.Nodes[1] = new ComputeNode(1);
            topology.Nodes[2] = new ComputeNode(2);

            // Neighborhoods
            topology.Nodes[0].Neighbors.Add(topology.Nodes[1]);
            topology.Nodes[0].Neighbors.Add(topology.Nodes[2]);
            topology.Nodes[1].Neighbors.Add(topology.Nodes[0]);
            topology.Nodes[1].Neighbors.Add(topology.Nodes[2]);
            topology.Nodes[2].Neighbors.Add(topology.Nodes[0]);
            topology.Nodes[2].Neighbors.Add(topology.Nodes[1]);

            return topology;
        }

        public Dictionary<ComputeNode, DistributedIndexer> CreateIndexers(IComputeEnvironment environment,
            ComputeNodeTopology topology)
        {
            var indexers = new Dictionary<ComputeNode, DistributedIndexer>();

            var indexer0 = new DistributedIndexer(topology.Nodes[0]);
            var commonEntries0 = new Dictionary<ComputeNode, int[]>();
            commonEntries0[topology.Nodes[2]] = new int[] { 0 };
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
            commonEntries2[topology.Nodes[0]] = new int[] { 2 };
            indexer2.Initialize(3, commonEntries2);
            indexers[indexer2.Node] = indexer2;

            Utilities.FilterNodeData(environment, indexers);
            return indexers;
        }

        public Dictionary<ComputeNode, int[]> CreateLocalToGlobalMaps(IComputeEnvironment environment, 
            ComputeNodeTopology topology)
        {
            var localToGlobalMaps = new Dictionary<ComputeNode, int[]>();
            localToGlobalMaps[topology.Nodes[0]] = new int[] { 0, 1, 2 };
            localToGlobalMaps[topology.Nodes[1]] = new int[] { 2, 3, 4 };
            localToGlobalMaps[topology.Nodes[2]] = new int[] { 4, 5, 0 };

            Utilities.FilterNodeData(environment, localToGlobalMaps);
            return localToGlobalMaps;
        }
    }
}
