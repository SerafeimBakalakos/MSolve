using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.Solvers.DDM
{
    /// <summary>
    /// Represents the common boundary nodes between two or more clusters. These entities do not have IDs, since those would
    /// require global communication to number uniquely.
    /// </summary>
    public class ClusterBoundary
    {
        public ClusterBoundary(IEnumerable<int> clusters)
        {
            this.Clusters = new HashSet<int>(clusters);
        }

        public HashSet<int> Clusters { get; }

        public SortedSet<int> Nodes { get; } = new SortedSet<int>();
    }
}
