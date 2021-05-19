using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.Environments;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.LinearAlgebra;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.Topologies;

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

namespace MGroup.Solvers_OLD.Tests.DistributedTry1.Distributed.LinearAlgebra
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

        public DistributedIndexer CreateIndexer(IComputeEnvironment environment, ComputeNodeTopology topology)
        {
            var indexer = new DistributedIndexer(topology.Nodes.Values);

            Action<ComputeNode> configIndexer = node =>
            {
                var commonEntries = new Dictionary<ComputeNode, int[]>();
                if (node.ID == 0)
                {
                    commonEntries[topology.Nodes[2]] = new int[] { 0 };
                    commonEntries[topology.Nodes[1]] = new int[] { 2 };
                }
                else if (node.ID == 1)
                {
                    commonEntries[topology.Nodes[0]] = new int[] { 0 };
                    commonEntries[topology.Nodes[2]] = new int[] { 2 };
                }
                else
                {
                    Debug.Assert(node.ID == 2);
                    commonEntries[topology.Nodes[1]] = new int[] { 0 };
                    commonEntries[topology.Nodes[0]] = new int[] { 2 };
                }
                indexer.ConfigureForNode(node, 3, commonEntries);
            };
            environment.DoPerNode(configIndexer);
            
            return indexer;
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
