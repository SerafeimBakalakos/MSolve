using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Cracks.Geometry;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Enrichment.Observers
{
    public class NewCrackTipNodesObserver : IEnrichmentObserver
    {
        private readonly ICrack crack;

        public NewCrackTipNodesObserver(ICrack crack)
        {
            this.crack = crack;
        }

        public HashSet<XNode> TipNodes { get; } = new HashSet<XNode>();

        public void Update(Dictionary<IEnrichment, XNode[]> enrichedNodes)
        {
            TipNodes.Clear();
            IEnrichment tipEnrichment = crack.CrackTipEnrichments[0]; // A node will be enriched with one or all of them.
            bool theyExist = enrichedNodes.TryGetValue(tipEnrichment, out XNode[] nodes);
            if (theyExist) TipNodes.UnionWith(nodes);
        }

        public IEnrichmentObserver[] RegisterAfterThese() => new IEnrichmentObserver[0];
    }
}
