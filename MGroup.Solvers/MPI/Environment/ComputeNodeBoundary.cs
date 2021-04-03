using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.Solvers.MPI.Environment
{
    public class ComputeNodeBoundary
    {
        public int Multiplicity => Nodes.Count;

        public List<ComputeNode> Nodes { get; }
    }
}
