using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.Solvers.Distributed.Topologies
{
    public class ComputeNode
    {
        public ComputeNode(int id)
        {
            ID = id;
        }

        public int ID { get; }

        public List<ComputeNode> Neighbors { get; } = new List<ComputeNode>();

        public override bool Equals(object obj)
        {
            return (obj is ComputeNode node) && (ID == node.ID);
        }

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

        public override int GetHashCode() => ID.GetHashCode();
    }
}
