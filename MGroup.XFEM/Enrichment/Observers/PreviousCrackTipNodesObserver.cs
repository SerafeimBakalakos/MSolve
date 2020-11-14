using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Cracks.Geometry;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Enrichment.Observers
{
    public class PreviousCrackTipNodesObserver : IEnrichmentObserver
    {
        private readonly ICrack crack;
        private readonly PreviousEnrichmentsObserver previousEnrichmentsObserver;

        public PreviousCrackTipNodesObserver(ICrack crack, PreviousEnrichmentsObserver previousEnrichmentsObserver)
        {
            this.crack = crack;
            this.previousEnrichmentsObserver = previousEnrichmentsObserver;
        }

        public HashSet<XNode> PreviousTipNodes { get; } = new HashSet<XNode>();

        public void Update(Dictionary<IEnrichment, XNode[]> enrichedNodes)
        {
            PreviousTipNodes.Clear();
            IEnrichment tipEnrichment = crack.CrackTipEnrichments[0]; // A node will be enriched with one or all of them.
            bool theyExist = previousEnrichmentsObserver.PreviousEnrichments.TryGetValue(tipEnrichment, out XNode[] nodes);
            if (theyExist) PreviousTipNodes.UnionWith(nodes);
        }

        public IEnrichmentObserver[] RegisterAfterThese() => new IEnrichmentObserver[] { previousEnrichmentsObserver };
    }
}
