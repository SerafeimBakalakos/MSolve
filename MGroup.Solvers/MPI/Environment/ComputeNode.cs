using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.Solvers.MPI.Environment
{
    public class ComputeNode
    {
        public ComputeNode(int id)
        {
            ID = id;
        }

        public List<ComputeNodeBoundary> Boundaries { get; } = new List<ComputeNodeBoundary>();

        public int ID { get; }

        public List<ComputeNode> Neighbors { get; } = new List<ComputeNode>();

        /// <summary>
        /// If <paramref name="other"/> is not a neighbor of this instance, then returns -1. Else returns the index of 
        /// <paramref name="other"/> into <see cref="Neighbors"/> of this instance. 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int FindNeighborIndex(ComputeNode other)
        {
            for (int i = 0; i < Neighbors.Count; ++i)
            {
                if (Neighbors[i] == other) return i;
            }
            return -1;
        }
    }
}
