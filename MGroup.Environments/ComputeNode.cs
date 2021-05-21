using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.Environments
{
    public class ComputeNode
    {
        public ComputeNode(int id)
        {
            ID = id;
        }

        public int ID { get; }

        public List<int> Neighbors { get; } = new List<int>();
    }
}
