using System;
using System.Collections.Generic;
using System.Text;

//TODO: Should I have a unique ID for each cluster boundary
namespace MGroup.Solvers.DDM
{
    public class ClusterBoundary
    {
        public List<Cluster> Clusters { get; } = new List<Cluster>();
    }
}
