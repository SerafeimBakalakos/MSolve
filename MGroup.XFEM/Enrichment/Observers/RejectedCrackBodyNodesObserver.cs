using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Cracks.Geometry;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Enrichment.Observers
{
    /// <summary>
    /// Tracks nodes that are not enriched with Heaviside functions, despite belonging to elements that intersect with the crack, 
    /// since that would cause singularities in the stiffness matrices.
    /// </summary>
    public class RejectedCrackBodyNodesObserver : IEnrichmentObserver
    {
        private readonly ICrack crack;
        private readonly NewCrackTipNodesObserver tipNodesObserver;

        public RejectedCrackBodyNodesObserver(ICrack crack, NewCrackTipNodesObserver tipNodesObserver)
        {
            this.crack = crack;
            this.tipNodesObserver = tipNodesObserver;
        }

        public HashSet<XNode> RejectedHeavisideNodes = new HashSet<XNode>();

        public IEnrichmentObserver[] RegisterAfterThese() => new IEnrichmentObserver[] { tipNodesObserver };

        public void Update(Dictionary<IEnrichment, XNode[]> enrichedNodes)
        {
            RejectedHeavisideNodes.Clear();
            var bodyElements = new HashSet<IXCrackElement>(crack.IntersectedElements);
            bodyElements.UnionWith(crack.ConformingElements);
            foreach (IXCrackElement element in bodyElements)
            {
                foreach (XNode node in element.Nodes)
                {
                    if (!tipNodesObserver.TipNodes.Contains(node))
                    {
                        RejectedHeavisideNodes.Add(node);
                    }
                }
            }
        }
    }
}
