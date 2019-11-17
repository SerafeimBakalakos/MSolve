using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM.Thermal.Elements;
using ISAAR.MSolve.XFEM.Thermal.Enrichments.Items;
using ISAAR.MSolve.XFEM.Thermal.Entities;
using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod;
using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod.MeshInteraction;
using ISAAR.MSolve.XFEM.Thermal.MaterialInterface.SingularityResolving;

namespace ISAAR.MSolve.XFEM.Thermal.MaterialInterface
{
    public class SingleMaterialInterface
    {
        private readonly IEnumerable<XThermalElement2D> modelElements;
        private readonly ILsmCurve2D curve;
        private readonly ThermalInterfaceEnrichment enrichment;
        private readonly IHeavisideSingularityResolver singularityResolver;

        public SingleMaterialInterface(GeometricModel2D geometricModel, ILsmCurve2D curve, 
            IEnumerable<XThermalElement2D> modelElements, double interfaceResistance) : 
            this(curve, modelElements, interfaceResistance, new RelativeAreaResolver(geometricModel))
        {
        }

        public SingleMaterialInterface(ILsmCurve2D curve, IEnumerable<XThermalElement2D> modelElements, 
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
            var intersectedElements = new List<IXFiniteElement>();
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
