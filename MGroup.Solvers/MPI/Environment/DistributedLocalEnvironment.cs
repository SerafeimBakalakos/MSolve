using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MGroup.Solvers.MPI.Environment
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

        public List<ComputeNode> ComputeNodes { get; } = new List<ComputeNode>();

        public bool AllReduceAnd(Dictionary<ComputeNode, bool> valuePerNode)
        {
            bool result = true;
            foreach (ComputeNode node in ComputeNodes)
            {
                result &= valuePerNode[node];
            }
            return result;
        }

        public double AllReduceSum(Dictionary<ComputeNode, double> valuePerNode)
        {
            double sum = 0.0;
            foreach (ComputeNode node in ComputeNodes)
            {
                sum += valuePerNode[node];
            }
            return sum;
        }

        public Dictionary<ComputeNode, T> CreateDictionary<T>(Func<ComputeNode, T> createDataPerNode)
        {
            var result = new Dictionary<ComputeNode, T>();
            foreach (ComputeNode node in ComputeNodes)
            {
                result[node] = createDataPerNode(node);
            }
            return result;
        }

        public void DoPerNode(Action<ComputeNode> action)
        {
            foreach (ComputeNode node in ComputeNodes)
            {
                action(node);
            }
        }

        public void NeighborhoodAllToAll(
            Dictionary<ComputeNode, (double[] inValues, int[] counts, double[] outValues)> dataPerNode)
        {
            foreach (ComputeNode thisNode in ComputeNodes)
            {
                (double[] thisInValues, int[] thisCounts, double[] thisOutValues) = dataPerNode[thisNode];

                for (int i = 0; i < thisNode.Neighbors.Count; ++i)
                {
                    // Receive data from each other node, by just copying the corresponding array segments.
                    ComputeNode otherNode = thisNode.Neighbors[i];
                    (double[] otherInValues, int[] otherCounts, double[] otherOutValues) = dataPerNode[otherNode];

                    // Find the start of the segment in the array of this node and the neighbor
                    int thisStart = FindOffset(thisNode, thisCounts, otherNode);
                    int otherStart = FindOffset(otherNode, otherCounts, thisNode);

                    // Find the number of entries both node will transfer and assert that they match
                    int thisCount = thisCounts[thisNode.FindNeighborIndex(otherNode)];
                    Debug.Assert(thisCount == otherCounts[otherNode.FindNeighborIndex(thisNode)]);

                    // Copy data from other to this node. 
                    // Copying from this to other node will be done in another iteration of the outer loop.
                    Array.Copy(otherInValues, otherStart, thisOutValues, thisStart, thisCount);
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
