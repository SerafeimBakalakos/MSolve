﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Cracks.Geometry;
using MGroup.XFEM.Cracks.Geometry.LSM;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Enrichment.Functions;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Elements
{
    public interface IXCrackElement : IXFiniteElement
    {
        
        Dictionary<ICrack, IElementCrackInteraction> InteractingCracks { get; }

        Matrix CalcDisplacementFieldGradient(XPoint point, Vector nodalDisplacements);
    }

    //TODO: These should be converted to default interface implementations
    public static class XCrackElementExtensions
    {
        public static bool HasTipEnrichedNodes(this IXCrackElement element)
        {
            foreach (XNode node in element.Nodes)
            {
                foreach (IEnrichmentFunction enrichment in node.EnrichmentFuncs.Keys)
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

        public static void RegisterInteractionWithCrack(this IXCrackElement element, 
            ICrack crack, IElementCrackInteraction interaction)
        {
            element.InteractingCracks[crack] = interaction;
            element.InteractingDiscontinuities[crack.ID] = interaction;
        }
    }
}
