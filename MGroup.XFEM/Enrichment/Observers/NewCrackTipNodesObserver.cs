using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Cracks.Geometry;
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

        public void Update(IEnumerable<EnrichmentItem> allEnrichments)
        {
            TipNodes.Clear();
            TipNodes.UnionWith(crack.CrackTipEnrichments.EnrichedNodes);
        }

        public IEnrichmentObserver[] RegisterAfterThese() => new IEnrichmentObserver[0];
    }
}
