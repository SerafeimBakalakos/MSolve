using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Enrichment.Observers
{
    /// <summary>
    /// Tracks elements with nodes with different enrichments or different values of enrichment functions between two consecutive
    /// crack propagation steps.    
    /// </summary>
    public class ElementsWithModifiedNodesObserver : IEnrichmentObserver
    {
        private readonly NodesWithModifiedEnrichmentsObserver nodesWithModifiedEnrichmentsObserver;

        public ElementsWithModifiedNodesObserver(NodesWithModifiedEnrichmentsObserver nodesWithModifiedEnrichmentsObserver)
        { 
            this.nodesWithModifiedEnrichmentsObserver = nodesWithModifiedEnrichmentsObserver;
        }

        public HashSet<IXFiniteElement> ModifiedElements { get; } = new HashSet<IXFiniteElement>();

        public void Update(Dictionary<IEnrichment, XNode[]> enrichedNodes)
        {
            ModifiedElements.Clear();
            foreach (XNode node in nodesWithModifiedEnrichmentsObserver.ModifiedNodes)
            {
                ModifiedElements.UnionWith(node.ElementsDictionary.Values);
            }
        }

        public IEnrichmentObserver[] RegisterAfterThese()
        {
            return new IEnrichmentObserver[] { nodesWithModifiedEnrichmentsObserver };
        }
    }
}
