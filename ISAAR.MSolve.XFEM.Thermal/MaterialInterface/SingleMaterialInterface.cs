using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM.Thermal.Elements;
using ISAAR.MSolve.XFEM.Thermal.Enrichments.Items;
using ISAAR.MSolve.XFEM.Thermal.Entities;
using ISAAR.MSolve.XFEM.Thermal.MaterialInterface.Geometry;

namespace ISAAR.MSolve.XFEM.Thermal.MaterialInterface
{
    public class SingleMaterialInterface
    {
        private readonly IEnumerable<XThermalElement2D> modelElements;
        private readonly IMaterialInterfaceGeometry geometry;
        private readonly ThermalInterfaceEnrichment enrichment;

        public SingleMaterialInterface(IMaterialInterfaceGeometry geometry, IEnumerable<XThermalElement2D> modelElements, 
            double interfaceResistance)
        {
            this.geometry = geometry;
            this.modelElements = modelElements;
            this.enrichment = new ThermalInterfaceEnrichment(geometry, interfaceResistance);
        }

        public void ApplyEnrichments()
        {
            // Find elements that are intersected by the discontinuity
            var intersectedElements = new List<IXFiniteElement>();
            foreach (XThermalElement2D element in modelElements)
            {
                bool isIntersected = geometry.IsElementIntersected(element);
                if (isIntersected)
                {
                    intersectedElements.Add(element);
                    element.EnrichmentItems.Add(enrichment);
                }
            }

            // Calculate and store nodal enrichments
            var enrichedNodes = new HashSet<XNode>();
            foreach (XThermalElement2D element in intersectedElements) enrichedNodes.UnionWith(element.Nodes);
            foreach (XNode node in enrichedNodes)
            {
                node.EnrichmentItems[enrichment] = enrichment.EvaluateFunctionsAt(node);
            }
        }
    }
}
