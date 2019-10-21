using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM.Thermal.Elements;
using ISAAR.MSolve.XFEM.Thermal.Enrichments.Items;
using ISAAR.MSolve.XFEM.Thermal.Entities;
using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod;
using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod.MeshInteraction;

namespace ISAAR.MSolve.XFEM.Thermal.MaterialInterface
{
    public class SingleMaterialInterface
    {
        private readonly IEnumerable<XThermalElement2D> modelElements;
        private readonly ILsmCurve2D geometry;
        private readonly ThermalInterfaceEnrichment enrichment;

        public SingleMaterialInterface(ILsmCurve2D geometry, IEnumerable<XThermalElement2D> modelElements, 
            double interfaceResistance)
        {
            this.geometry = geometry;
            this.modelElements = modelElements;
            this.enrichment = new ThermalInterfaceEnrichment(geometry, interfaceResistance);
        }

        public void ApplyEnrichments()
        {
            // Find elements that interact with the discontinuity. Their nodes will be enriched.
            var intersectedElements = new List<IXFiniteElement>();
            var enrichedNodes = new HashSet<XNode>();

            foreach (XThermalElement2D element in modelElements) //TODO: Better to ask the LSM to return the relevant elements
            {
                CurveElementIntersection intersection = geometry.IntersectElement(element);
                if (intersection.RelativePosition == RelativePositionCurveElement.Intersection)
                {
                    intersectedElements.Add(element);
                    element.EnrichmentItems.Add(enrichment);
                    enrichedNodes.UnionWith(element.Nodes);
                }
                else if (intersection.RelativePosition == RelativePositionCurveElement.TangentAtSingleNode 
                    || intersection.RelativePosition == RelativePositionCurveElement.TangentAlongElementEdge)
                {
                    enrichedNodes.UnionWith(intersection.ContactNodes);
                }
            }

            // Calculate and store nodal enrichments
            foreach (XNode node in enrichedNodes)
            {
                node.EnrichmentItems[enrichment] = enrichment.EvaluateFunctionsAt(node);
            }
        }
    }
}
