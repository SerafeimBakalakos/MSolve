﻿using System;
using System.Collections.Generic;
using System.Text;
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
            topology.Boundaries[0] = new ComputeNodeBoundary(0, new ComputeNode[] { topology.Nodes[0], topology.Nodes[1] });
            topology.Boundaries[1] = new ComputeNodeBoundary(1, new ComputeNode[] { topology.Nodes[1], topology.Nodes[2] });
            topology.Boundaries[2] = new ComputeNodeBoundary(2, new ComputeNode[] { topology.Nodes[2], topology.Nodes[3] });
            topology.ConnectData();

            return topology;
        }

        public Dictionary<ComputeNode, DistributedIndexer> CreateIndexers(ComputeNodeTopology topology)
        {
            var indexers = new Dictionary<ComputeNode, DistributedIndexer>();

            var indexer0 = new DistributedIndexer(topology.Nodes[0]);
            var boundaryEntries0 = new Dictionary<ComputeNodeBoundary, int[]>();
            boundaryEntries0[topology.Boundaries[0]] = new int[] { 2 };
            indexer0.Initialize(3, boundaryEntries0);
            indexers[indexer0.Node] = indexer0;

            var indexer1 = new DistributedIndexer(topology.Nodes[1]);
            var boundaryEntries1 = new Dictionary<ComputeNodeBoundary, int[]>();
            boundaryEntries1[topology.Boundaries[0]] = new int[] { 0 };
            boundaryEntries1[topology.Boundaries[1]] = new int[] { 2 };
            indexer1.Initialize(3, boundaryEntries1);
            indexers[indexer1.Node] = indexer1;

            var indexer2 = new DistributedIndexer(topology.Nodes[2]);
            var boundaryEntries2 = new Dictionary<ComputeNodeBoundary, int[]>();
            boundaryEntries2[topology.Boundaries[1]] = new int[] { 0 };
            boundaryEntries2[topology.Boundaries[2]] = new int[] { 2 };
            indexer2.Initialize(3, boundaryEntries2);
            indexers[indexer2.Node] = indexer2;

            var indexer3 = new DistributedIndexer(topology.Nodes[3]);
            var boundaryEntries3 = new Dictionary<ComputeNodeBoundary, int[]>();
            boundaryEntries3[topology.Boundaries[2]] = new int[] { 0 };
            indexer3.Initialize(3, boundaryEntries3);
            indexers[indexer3.Node] = indexer3;

            return indexers;
        }

        public Dictionary<ComputeNode, int[]> CreateLocalToGlobalMaps(ComputeNodeTopology topology)
        {
            var localToGlobalMaps = new Dictionary<ComputeNode, int[]>();
            localToGlobalMaps[topology.Nodes[0]] = new int[] { 0, 1, 2 };
            localToGlobalMaps[topology.Nodes[1]] = new int[] { 2, 3, 4 };
            localToGlobalMaps[topology.Nodes[2]] = new int[] { 4, 5, 6 };
            localToGlobalMaps[topology.Nodes[3]] = new int[] { 6, 7, 8 };
            return localToGlobalMaps;
        }
    }
}
