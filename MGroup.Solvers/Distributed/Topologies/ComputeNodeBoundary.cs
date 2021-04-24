using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.Solvers.Distributed.Topologies
{
    public class ComputeNodeBoundary
    {
        public ComputeNodeBoundary(int id, IEnumerable<ComputeNode> nodes)
        {
            this.ID = id;
            this.Nodes = new List<ComputeNode>(nodes);
        }

        public int ID { get; }

        public int Multiplicity => Nodes.Count;

        public List<ComputeNode> Nodes { get; }

        public override bool Equals(object obj)
        {
            return (obj is ComputeNodeBoundary boundary) && (ID == boundary.ID);
        }

        public override int GetHashCode() => ID.GetHashCode();
    }
}
