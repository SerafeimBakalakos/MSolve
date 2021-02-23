using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Elements;

namespace MGroup.XFEM.Geometry.LSM.Utilities
{
    /// <summary>
    /// Level set values for nodes of a specific element. These are faster to read than from the Dictionaries containing all
    /// the level sets of all nodes of the mesh. The nodes are identified by their ID.
    /// </summary>
    public class ElementLevelSets
    {
        public ElementLevelSets(IXFiniteElement element, 
            Dictionary<int, double> allBodyLevelSets, Dictionary<int, double> allTipLevelSets)
        {
            BodyLevelSets = new Dictionary<int, double>();
            TipLevelSets = new Dictionary<int, double>();

            int numNodes = element.Nodes.Count;
            for (int n = 0; n < numNodes; ++n)
            {
                int nodeID = element.Nodes[n].ID;
                this.BodyLevelSets[nodeID] = allBodyLevelSets[nodeID];
                this.TipLevelSets[nodeID] = allTipLevelSets[nodeID];
            }
        }

        public Dictionary<int, double> BodyLevelSets { get; }

        public Dictionary<int, double> TipLevelSets { get; }
    }
}
