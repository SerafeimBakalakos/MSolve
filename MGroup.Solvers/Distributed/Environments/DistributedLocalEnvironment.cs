using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MGroup.Solvers.Distributed.Topologies;

namespace MGroup.Solvers.Distributed.Environments
{
    public class DistributedLocalEnvironment : IComputeEnvironment
    {
        private readonly bool duplicateCommonData;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="duplicateCommonData">
        /// If true, then data that are identical across multiple nodes will be copied and each node will have a different 
        /// instance. If false, then the instance of common data will be shared across all nodes. E.g. when broadcasting data.
        /// </param>
        public DistributedLocalEnvironment(bool duplicateCommonData = true)
        {
            this.duplicateCommonData = duplicateCommonData;
        }

        public ComputeNodeTopology NodeTopology { get; set; }

        public bool AllReduceAnd(Dictionary<ComputeNode, bool> valuePerNode)
        {
            bool result = true;
            foreach (ComputeNode node in NodeTopology.Nodes.Values)
            {
                result &= valuePerNode[node];
            }
            return result;
        }

        public double AllReduceSum(Dictionary<ComputeNode, double> valuePerNode)
        {
            double sum = 0.0;
            foreach (ComputeNode node in NodeTopology.Nodes.Values)
            {
                sum += valuePerNode[node];
            }
            return sum;
        }

        public Dictionary<ComputeNode, T> CreateDictionary<T>(Func<ComputeNode, T> createDataPerNode)
        {
            var result = new Dictionary<ComputeNode, T>();
            foreach (ComputeNode node in NodeTopology.Nodes.Values)
            {
                result[node] = createDataPerNode(node);
            }
            return result;
        }

        public void DoPerNode(Action<ComputeNode> action)
        {
            foreach (ComputeNode node in NodeTopology.Nodes.Values)
            {
                action(node);
            }
        }

        public void NeighborhoodAllToAll(Dictionary<ComputeNode, AllToAllNodeData> dataPerNode)
        {
            foreach (ComputeNode thisNode in NodeTopology.Nodes.Values)
            {
                AllToAllNodeData thisData = dataPerNode[thisNode];

                for (int i = 0; i < thisNode.Neighbors.Count; ++i)
                {
                    // Receive data from each other node, by just copying the corresponding array segments.
                    ComputeNode otherNode = thisNode.Neighbors[i];
                    AllToAllNodeData otherData = dataPerNode[otherNode];

                    // Find the start of the segment in the array of this node and the neighbor
                    int thisStart = FindOffset(thisNode, thisData.sendRecvCounts, otherNode);
                    int otherStart = FindOffset(otherNode, otherData.sendRecvCounts, thisNode);

                    // Find the number of entries both node will transfer and assert that they match
                    int thisCount = thisData.sendRecvCounts[thisNode.FindNeighborIndex(otherNode)];
                    Debug.Assert(thisCount == otherData.sendRecvCounts[otherNode.FindNeighborIndex(thisNode)]);

                    // Copy data from other to this node. 
                    // Copying from this to other node will be done in another iteration of the outer loop.
                    Array.Copy(otherData.sendValues, otherStart, thisData.recvValues, thisStart, thisCount);
                }
            }
        }

        private static int FindOffset(ComputeNode thisNode, int[] entryCountPerNeighborOfThis, ComputeNode otherNode)
        {
            int offset = 0;
            for (int i = 0; i < thisNode.Neighbors.Count; ++i)
            {
                if (otherNode == thisNode.Neighbors[i])
                {
                    return offset;
                }
                else
                {
                    offset += entryCountPerNeighborOfThis[i];
                }
            }
            throw new ArgumentException("The provided compute nodes are not neighbors");
        }
    }
}
