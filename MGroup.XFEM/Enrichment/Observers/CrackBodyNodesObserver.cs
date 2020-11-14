using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Cracks.Geometry;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Enrichment.Observers
{
    public class CrackBodyNodesObserver : IEnrichmentObserver
    {
        private readonly ICrack crack;

        public CrackBodyNodesObserver(ICrack crack)
        {
            this.crack = crack;
        }

        public HashSet<XNode> BodyNodes { get; } = new HashSet<XNode>();

        public void Update(Dictionary<IEnrichment, XNode[]> enrichedNodes)
        {
            BodyNodes.Clear();
            bool theyExist = enrichedNodes.TryGetValue(crack.CrackBodyEnrichment, out XNode[] nodes);
            if (theyExist) BodyNodes.UnionWith(nodes);
        }
        
        public IEnrichmentObserver[] RegisterAfterThese() => new IEnrichmentObserver[0];
    }
}
