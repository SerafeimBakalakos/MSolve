using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.Solvers_OLD.DistributedTry1.Distributed.Topologies
{
    public class ComputeNode
    {
        public ComputeNode(int id)
        {
            ID = id;
        }

        public int ID { get; }

        //TODOMPI: these should probably be the ids only, to avoid depending on data that are unavailable in this memory address space.
        public List<ComputeNode> Neighbors { get; } = new List<ComputeNode>();

        public Dictionary<int, ComputeSubnode> Subnodes { get; } = new Dictionary<int, ComputeSubnode>();

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
