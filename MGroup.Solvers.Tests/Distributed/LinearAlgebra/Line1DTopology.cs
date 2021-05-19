using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.Environments;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.LinearAlgebra;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.Topologies;

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
namespace MGroup.Solvers_OLD.Tests.DistributedTry1.Distributed.LinearAlgebra
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

        public DistributedIndexer CreateIndexer(IComputeEnvironment environment, ComputeNodeTopology topology)
        {
            var indexer = new DistributedIndexer(topology.Nodes.Values);

            Action<ComputeNode> configIndexer = node =>
            {
                var commonEntries = new Dictionary<ComputeNode, int[]>();
                if (node.ID == 0)
                {
                    commonEntries[topology.Nodes[1]] = new int[] { 2 };
                }
                else if (node.ID == 1)
                {
                    commonEntries[topology.Nodes[0]] = new int[] { 0 };
                    commonEntries[topology.Nodes[2]] = new int[] { 2 };
                }
                else if (node.ID == 2)
                {
                    commonEntries[topology.Nodes[1]] = new int[] { 0 };
                    commonEntries[topology.Nodes[3]] = new int[] { 2 };
                }
                else
                {
                    Debug.Assert(node.ID == 3);
                    commonEntries[topology.Nodes[2]] = new int[] { 0 };
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
            localToGlobalMaps[topology.Nodes[2]] = new int[] { 4, 5, 6 };
            localToGlobalMaps[topology.Nodes[3]] = new int[] { 6, 7, 8 };

            Utilities.FilterNodeData(environment, localToGlobalMaps);
            return localToGlobalMaps;
        }
    }
}
