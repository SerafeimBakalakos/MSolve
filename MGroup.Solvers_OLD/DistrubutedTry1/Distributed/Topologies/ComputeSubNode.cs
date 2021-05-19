using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.Solvers_OLD.DistributedTry1.Distributed.Topologies
{
    public class ComputeSubnode
    {
        public ComputeSubnode(int id)
        {
            this.ID = id;
        }

        public int ID { get; }

        public ComputeNode ParentNode { get; set; }
    }
}
