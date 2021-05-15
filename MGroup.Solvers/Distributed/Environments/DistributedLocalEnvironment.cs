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
        public DistributedLocalEnvironment(int numComputeNodes, bool duplicateCommonData = true)
        {
            this.NumComputeNodes = numComputeNodes;
            this.duplicateCommonData = duplicateCommonData;
        }

        public ComputeNodeTopology NodeTopology { get; set; }

        public int NumComputeNodes { get; }

        public T AccessNodeDataFromSubnode<T>(ComputeSubnode subnode, Func<ComputeNode, T> getNodeData)
            => getNodeData(subnode.ParentNode);

        public T AccessSubnodeDataFromNode<T>(ComputeSubnode subnode, Func<ComputeSubnode, T> getSubnodeData)
            => getSubnodeData(subnode);

        public bool AllReduceAndForNodes(Dictionary<ComputeNode, bool> valuePerNode)
        {
            bool result = true;
            foreach (ComputeNode node in NodeTopology.Nodes.Values)
            {
                result &= valuePerNode[node];
            }
            return result;
        }

        public double AllReduceSumForNodes(Dictionary<ComputeNode, double> valuePerNode)
        {
            double sum = 0.0;
            foreach (ComputeNode node in NodeTopology.Nodes.Values)
            {
                sum += valuePerNode[node];
            }
            return sum;
        }

        public Dictionary<ComputeNode, T> CreateDictionaryPerNode<T>(Func<ComputeNode, T> createDataPerNode)
        {
            var result = new Dictionary<ComputeNode, T>();
            foreach (ComputeNode node in NodeTopology.Nodes.Values)
            {
                result[node] = createDataPerNode(node);
            }
            return result;
        }

        public Dictionary<int, T> CreateDictionaryPerSubnode<T>(Func<ComputeSubnode, T> createDataPerSubnode)
        {
            var result = new Dictionary<int, T>();
            foreach (ComputeNode node in NodeTopology.Nodes.Values)
            {
                foreach (ComputeSubnode subnode in node.Subnodes.Values)
                {
                    result[subnode.ID] = createDataPerSubnode(subnode);
                }
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

        public void DoPerSubnode(Action<ComputeSubnode> action)
        {
            foreach (ComputeNode node in NodeTopology.Nodes.Values)
            {
                foreach (ComputeSubnode subnode in node.Subnodes.Values)
                {
                    action(subnode);
                }
            }
        }

        public void NeighborhoodAllToAllForNodes<T>(Dictionary<ComputeNode, AllToAllNodeData<T>> dataPerNode, bool areRecvBuffersKnown)
        {
            foreach (ComputeNode thisNode in NodeTopology.Nodes.Values)
            {
                AllToAllNodeData<T> thisData = dataPerNode[thisNode];

                for (int i = 0; i < thisNode.Neighbors.Count; ++i)
                {
                    // Receive data from each other node, by just copying the corresponding array segments.
                    ComputeNode otherNode = thisNode.Neighbors[i];
                    AllToAllNodeData<T> otherData = dataPerNode[otherNode];
                    int thisNeighborIdx = thisNode.FindNeighborIndex(otherNode);
                    int otherNeighborIdx = otherNode.FindNeighborIndex(thisNode);
                    int bufferLength = otherData.sendValues[otherNeighborIdx].Length;

                    if (!areRecvBuffersKnown)
                    {
                        Debug.Assert(thisData.recvValues == null, "This buffer must not exist previously.");
                        thisData.recvValues[thisNeighborIdx] = new T[bufferLength];
                    }
                    else
                    {
                        Debug.Assert(thisData.recvValues[thisNeighborIdx].Length == bufferLength,
                            $"Node {otherNode.ID} tries to send {bufferLength} entries but node {thisNode.ID} tries to" +
                                $" receive {thisData.recvValues[thisNeighborIdx].Length} entries. They must match.");
                    }

                    // Copy data from other to this node. 
                    // Copying from this to other node will be done in another iteration of the outer loop.
                    Array.Copy(otherData.sendValues[otherNeighborIdx], thisData.recvValues[thisNeighborIdx], bufferLength);
                }
            }
        }

        public void NeighborhoodAllToAllForNodes(Dictionary<ComputeNode, AllToAllNodeData> dataPerNode)
        {
            foreach (ComputeNode thisNode in NodeTopology.Nodes.Values)
            {
                AllToAllNodeData thisData = dataPerNode[thisNode];

                for (int i = 0; i < thisNode.Neighbors.Count; ++i)
                {
                    // Receive data from each other node, by just copying the corresponding array segments.
                    ComputeNode otherNode = thisNode.Neighbors[i];
                    AllToAllNodeData otherData = dataPerNode[otherNode];
                    double[] otherSendValues = otherData.sendValues[otherNode.FindNeighborIndex(thisNode)];
                    double[] thisRecvValues = thisData.recvValues[thisNode.FindNeighborIndex(otherNode)];

#if DEBUG
                    if (otherSendValues.Length != thisRecvValues.Length)
                    {
                        throw new ArgumentException($"Node {otherNode.ID} tries to send {otherSendValues.Length} entries" +
                            $" but node {thisNode.ID} tries to receive {thisRecvValues.Length} entries. They must match.");
                    }
#endif

                    // Copy data from other to this node. 
                    // Copying from this to other node will be done in another iteration of the outer loop.
                    Array.Copy(otherSendValues, thisRecvValues, thisRecvValues.Length);
                }
            }
        }

        public void NeighborhoodAllToAllForNodes(Dictionary<ComputeNode, AllToAllNodeDataEntire> dataPerNode)
        {
            foreach (ComputeNode thisNode in NodeTopology.Nodes.Values)
            {
                AllToAllNodeDataEntire thisData = dataPerNode[thisNode];

                for (int i = 0; i < thisNode.Neighbors.Count; ++i)
                {
                    // Receive data from each other node, by just copying the corresponding array segments.
                    ComputeNode otherNode = thisNode.Neighbors[i];
                    AllToAllNodeDataEntire otherData = dataPerNode[otherNode];

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
