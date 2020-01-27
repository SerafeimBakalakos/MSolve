using System.Collections.Generic;
using ISAAR.MSolve.XFEM.ThermalOLD.Elements;
using ISAAR.MSolve.XFEM.ThermalOLD.Enrichments.Items;
using ISAAR.MSolve.XFEM.ThermalOLD.Entities;
using ISAAR.MSolve.XFEM.ThermalOLD.Curves;
using ISAAR.MSolve.XFEM.ThermalOLD.Curves.MeshInteraction;
using ISAAR.MSolve.XFEM.ThermalOLD.MaterialInterface.SingularityResolving;

namespace ISAAR.MSolve.XFEM.ThermalOLD.MaterialInterface
{
    public class SingleMaterialInterfaceEnricher
    {
        private readonly IEnumerable<XThermalElement2D> modelElements;
        private readonly ICurve2D curve;
        private readonly ThermalInterfaceEnrichment enrichment;
        private readonly IHeavisideSingularityResolver singularityResolver;

        public SingleMaterialInterfaceEnricher(GeometricModel2D geometricModel, ICurve2D curve, 
            IEnumerable<XThermalElement2D> modelElements, double interfaceResistance) : 
            this(curve, modelElements, interfaceResistance, new RelativeAreaResolver(geometricModel))
        {
        }

        public SingleMaterialInterfaceEnricher(ICurve2D curve, IEnumerable<XThermalElement2D> modelElements, 
            double interfaceResistance, IHeavisideSingularityResolver singularityResolver)
        {
            this.curve = curve;
            this.modelElements = modelElements;
            this.enrichment = new ThermalInterfaceEnrichment(curve, interfaceResistance);
            this.singularityResolver = singularityResolver;
        }

        public void ApplyEnrichments()
        {
            // Find elements that interact with the discontinuity. Their nodes will be enriched.
            var intersectedElements = new List<IXFiniteElement>(); //TODO: This is unused
            var enrichedNodes = new HashSet<XNode>();

            foreach (XThermalElement2D element in modelElements) //TODO: Better to ask the LSM to return the relevant elements
            {
                CurveElementIntersection intersection = curve.IntersectElement(element);
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

            // Remove nodes whose nodal support does not include Gauss points in both sides of the discontinuity
            HashSet<XNode> nodesToRemove = singularityResolver.FindHeavisideNodesToRemove(curve, enrichedNodes);
            enrichedNodes.ExceptWith(nodesToRemove);

            // Calculate and store nodal enrichments
            foreach (XNode node in enrichedNodes)
            {
                node.EnrichmentItems[enrichment] = enrichment.EvaluateFunctionsAt(node);
            }
        }
    }
}
