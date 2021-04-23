using System;
using System.Collections.Generic;
using System.Text;
using MGroup.Solvers.MPI.Environments;
using MGroup.Solvers.MPI.LinearAlgebra;
using MGroup.Solvers.MPI.Topologies;

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

namespace MGroup.Solvers.Tests.MPI.LinearAlgebra
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
            topology.Boundaries[0] = new ComputeNodeBoundary(0, new ComputeNode[] { topology.Nodes[2], topology.Nodes[0] });
            topology.Boundaries[1] = new ComputeNodeBoundary(1, new ComputeNode[] { topology.Nodes[0], topology.Nodes[1] });
            topology.Boundaries[2] = new ComputeNodeBoundary(2, new ComputeNode[] { topology.Nodes[1], topology.Nodes[2] });
            topology.ConnectData();

            return topology;
        }

        public Dictionary<ComputeNode, DistributedIndexer> CreateIndexers(ComputeNodeTopology topology)
        {
            var indexers = new Dictionary<ComputeNode, DistributedIndexer>();

            var indexer0 = new DistributedIndexer(topology.Nodes[0]);
            var boundaryEntries0 = new Dictionary<ComputeNodeBoundary, int[]>();
            boundaryEntries0[topology.Boundaries[0]] = new int[] { 0 };
            boundaryEntries0[topology.Boundaries[1]] = new int[] { 2 };
            indexer0.Initialize(3, boundaryEntries0);
            indexers[indexer0.Node] = indexer0;

            var indexer1 = new DistributedIndexer(topology.Nodes[1]);
            var boundaryEntries1 = new Dictionary<ComputeNodeBoundary, int[]>();
            boundaryEntries1[topology.Boundaries[1]] = new int[] { 0 };
            boundaryEntries1[topology.Boundaries[2]] = new int[] { 2 };
            indexer1.Initialize(3, boundaryEntries1);
            indexers[indexer1.Node] = indexer1;

            var indexer2 = new DistributedIndexer(topology.Nodes[2]);
            var boundaryEntries2 = new Dictionary<ComputeNodeBoundary, int[]>();
            boundaryEntries2[topology.Boundaries[2]] = new int[] { 0 };
            boundaryEntries2[topology.Boundaries[0]] = new int[] { 2 };
            indexer2.Initialize(3, boundaryEntries2);
            indexers[indexer2.Node] = indexer2;

            return indexers;
        }

        public Dictionary<ComputeNode, int[]> CreateLocalToGlobalMaps(ComputeNodeTopology topology)
        {
            var localToGlobalMaps = new Dictionary<ComputeNode, int[]>();
            localToGlobalMaps[topology.Nodes[0]] = new int[] { 0, 1, 2 };
            localToGlobalMaps[topology.Nodes[1]] = new int[] { 2, 3, 4 };
            localToGlobalMaps[topology.Nodes[2]] = new int[] { 4, 5, 0 };
            return localToGlobalMaps;
        }
    }
}
