using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Enrichment.Observers
{
    /// <summary>
    /// This may substantially increase the memory requirements, since it stores two copies of the enrichments-nodes 
    /// relationships.
    /// </summary>
    public class PreviousEnrichmentsObserver : IEnrichmentObserver
    {
        public Dictionary<IEnrichment, XNode[]> PreviousEnrichments { get; private set; }
        private Dictionary<IEnrichment, XNode[]> CurrentEnrichments { get; set; }

        public void Update(Dictionary<IEnrichment, XNode[]> enrichedNodes)
        {
            PreviousEnrichments = CurrentEnrichments;
            CurrentEnrichments = Copy(enrichedNodes);
        }

        public IEnrichmentObserver[] RegisterAfterThese() => new IEnrichmentObserver[0];

        private static Dictionary<IEnrichment, XNode[]> Copy(Dictionary<IEnrichment, XNode[]> enrichedNodes)
        {
            var copy = new Dictionary<IEnrichment, XNode[]>();
            foreach (var pair in enrichedNodes)
            {
                XNode[] nodes = pair.Value;
                var nodesCopy = new XNode[nodes.Length];
                Array.Copy(nodes, nodesCopy, nodes.Length);
                copy[pair.Key] = nodesCopy;
            }
            return copy;
        }
    }
}
