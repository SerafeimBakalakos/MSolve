using System;
using System.Collections.Generic;
using System.Text;

//TODOMPI: Add CartesianTopology2D and CartesianTopology3D to assist such applications. Also facilitate the creation of 
//      DistributedIndexer in these cases. Finally add dedicated examples with these topologies. Each test class should contain 
//      vector and matrix tests, but for a specific topology.
namespace MGroup.Solvers_OLD.DistributedTry1.Distributed.Topologies
{
    public class ComputeNodeTopology
    {
        public ComputeNodeTopology()
        { 
        }

        public Dictionary<int, ComputeNode> Nodes { get; } = new Dictionary<int, ComputeNode>();
    }
}
