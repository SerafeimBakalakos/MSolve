using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Cracks.Geometry;
using MGroup.XFEM.Cracks.Geometry.LSM;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Interpolation;

namespace MGroup.XFEM.Elements
{
    public interface IXCrackElement : IXFiniteElement
    {
        
        Dictionary<ICrack, IElementCrackInteraction> InteractingCracks { get; }

        //TODO: Delete these implementations. They are covered by InteractingCracks, which works better in the presence of multiple cracks
        //bool IsIntersectedElement { get; set; } //TODO: Should this be in IXFiniteElement? It is similar to storing the triangulation mesh.
        //bool IsTipElement { get; set; }

        Matrix CalcDisplacementFieldGradient(XPoint point, Vector nodalDisplacements);
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

        public static bool IsIntersected(this IXCrackElement element)
        {
            if (element.InteractingCracks.Count == 0) return false;
            else if (element.InteractingCracks.Count == 1)
            {
                IElementCrackInteraction interaction = element.InteractingCracks.First().Value;
                return interaction.RelativePosition == RelativePositionCurveElement.Intersecting;
            }
            else
            {
                throw new NotImplementedException("For now only 1 crack may interact with each element");
            }
        }

        public static bool IsTipElement(this IXCrackElement element)
        {
            if (element.InteractingCracks.Count == 0) return false;
            else if (element.InteractingCracks.Count == 1)
            {
                IElementCrackInteraction interaction = element.InteractingCracks.First().Value;
                return interaction.TipInteractsWithElement;
            }
            else
            {
                throw new NotImplementedException("For now only 1 crack may interact with each element");
            }
        }
    }
}
