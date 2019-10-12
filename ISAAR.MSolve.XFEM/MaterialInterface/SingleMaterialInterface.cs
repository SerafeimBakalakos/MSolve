using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM.Elements;
using ISAAR.MSolve.XFEM.Enrichments.Items;
using ISAAR.MSolve.XFEM.Entities;

namespace ISAAR.MSolve.XFEM.MaterialInterface
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
            FindEnrichedNodesElements();
            ApplyEnrichmentFunctions();
        }

        private void ApplyEnrichmentFunctions()
        {
            throw new NotImplementedException();
        }

        private void FindEnrichedNodesElements()
        {
            // Find elements that are intersected by the discontinuity
            var intersectedElements = new List<IXFiniteElement>();
            foreach (XThermalElement2D element in modelElements)
            {
                bool isIntersected = geometry.IsElementIntersected(element);
                if (isIntersected) intersectedElements.Add(element);
            }

            // Enrich intersected elements and their nodes
            foreach (XThermalElement2D element in intersectedElements)
            {
                element.EnrichmentItems.Add(enrichment);
                foreach (XNode node in element.Nodes) /*node.EnrichmentItems[enrichment] = */; 
            }
        }
    }
}
