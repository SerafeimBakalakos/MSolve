using System;
using System.Collections.Generic;
using System.Text;

//TODOMPI: Add CartesianTopology2D and CartesianTopology3D to assist such applications. Also facilitate the creation of 
//      DistributedIndexer in these cases. Finally add dedicated examples with these topologies. Each test class should contain 
//      vector and matrix tests, but for a specific topology.
namespace MGroup.Solvers.MPI.Topologies
{
    public class ComputeNodeTopology
    {
        public ComputeNodeTopology()
        { 
        }

        public Dictionary<int, ComputeNodeBoundary> Boundaries { get; } = new Dictionary<int, ComputeNodeBoundary>();

        public Dictionary<int, ComputeNode> Nodes { get; } = new Dictionary<int, ComputeNode>();


        public void ConnectData()
        {
            foreach (ComputeNode node in Nodes.Values)
            {
                node.Neighbors.Clear();
                node.Boundaries.Clear();
            }

            // Inform nodes about their boundaries
            foreach (ComputeNodeBoundary boundary in Boundaries.Values)
            {
                foreach (ComputeNode node in boundary.Nodes)
                {
                    node.Boundaries.Add(boundary);
                }
            }

            // Inform nodes about their neighbors
            foreach (ComputeNode node in Nodes.Values)
            {
                var neighbors = new SortedDictionary<int, ComputeNode>();
                foreach (ComputeNodeBoundary boundary in node.Boundaries)
                {
                    foreach (ComputeNode otherNode in boundary.Nodes)
                    {
                        if (otherNode != node) neighbors[otherNode.ID] = otherNode;
                    }
                }
                node.Neighbors.AddRange(neighbors.Values);
            }

        }
    }
}
