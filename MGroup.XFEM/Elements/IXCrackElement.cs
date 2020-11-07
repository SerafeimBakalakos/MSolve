using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Elements
{
    public interface IXCrackElement : IXFiniteElement
    {
        bool IsIntersectedElement { get; set; } //TODO: Should this be in IXFiniteElement? It is similar to storing the triangulation mesh.

        bool IsTipElement { get; set; }
    }

    //TODO: These should be converted to default interface implementations
    public static class XCrackElementExtensions
    {
        public static bool HasTipEnrichedNodes(this IXCrackElement element)
        {
            foreach (XNode node in element.Nodes)
            {
                foreach (IEnrichment enrichment in node.Enrichments.Keys)
                {
                    if (enrichment is ICrackTipEnrichment) return true;
                }
            }
            return false;
        }
    }
}
