using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Cracks.Geometry.LSM;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Enrichment.Observers
{
    /// <summary>
    /// Tracks nodes that are enriched with crack body enrichments, but their corresponding crack body level sets were modified
    /// with respect to their values during the previous propagation step.
    /// </summary>
    public class CrackBodyNodesWithModifiedLevelSetObserver : IEnrichmentObserver
    {
        private readonly ExteriorLsmCrack crack;
        private readonly CrackBodyNodesObserver bodyNodesObserver;

        public CrackBodyNodesWithModifiedLevelSetObserver(ExteriorLsmCrack crack, CrackBodyNodesObserver bodyNodesObserver)
        {
            this.crack = crack;
            this.bodyNodesObserver = bodyNodesObserver;
        }

        /// <summary>
        /// These may be different from the crack body level sets provided by the crack itself, since they may correspond to the
        /// previous propagation step, depending on when they are accessed.
        /// </summary>
        public HashSet<XNode> BodyNodesWithModifiedLevelSets { get; } = new HashSet<XNode>();

        public Dictionary<int, double> LevelSetsOfBodyNodes { get; private set; } = new Dictionary<int, double>();

        public void Update(Dictionary<IEnrichment, XNode[]> enrichedNodes)
        {
            Dictionary<int, double> previousBodyLevelSets = LevelSetsOfBodyNodes;
            LevelSetsOfBodyNodes = new Dictionary<int, double>();
            BodyNodesWithModifiedLevelSets.Clear();
            foreach (XNode node in bodyNodesObserver.BodyNodes)
            {
                double newLevelSet = crack.LsmGeometry.LevelSets[node.ID];
                LevelSetsOfBodyNodes[node.ID] = newLevelSet;
                if (newLevelSet != previousBodyLevelSets[node.ID]) BodyNodesWithModifiedLevelSets.Add(node);
            }
        }

        public IEnrichmentObserver[] RegisterAfterThese()
        {
            return new IEnrichmentObserver[] { bodyNodesObserver };
        }
    }
}
