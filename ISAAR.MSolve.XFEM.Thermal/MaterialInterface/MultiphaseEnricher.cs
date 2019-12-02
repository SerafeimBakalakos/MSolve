using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;
using ISAAR.MSolve.XFEM.Thermal.Elements;
using ISAAR.MSolve.XFEM.Thermal.Enrichments.Items;
using ISAAR.MSolve.XFEM.Thermal.Entities;
using ISAAR.MSolve.XFEM.Thermal.Curves.MeshInteraction;
using ISAAR.MSolve.XFEM.Thermal.MaterialInterface.SingularityResolving;

//TODO: Use IHeavisideSingularityResolver when selecting enriched nodes!!!
//TODO: Avoid processing all elements in the model to see if they must be enriched. Limit the efforts to the elements near the 
//      level sets.
namespace ISAAR.MSolve.XFEM.Thermal.MaterialInterface
{
    public class MultiphaseEnricher
    {
        private readonly GeometricModel2D geometricModel;
        private readonly ThermalInterfaceEnrichment[] heavisideEnrichments;
        private readonly IEnumerable<XThermalElement2D> modelElements;
        private readonly IHeavisideSingularityResolver singularityResolver;

        private Dictionary<XThermalElement2D, ThermalJunctionEnrichment> junctionEnrichments;

        public MultiphaseEnricher(GeometricModel2D geometricModel, IEnumerable<XThermalElement2D> modelElements,
            double[] interfaceResistances)
        {
            this.geometricModel = geometricModel;
            this.modelElements = modelElements;
            singularityResolver = new RelativeAreaResolver(geometricModel);

            int numCurves = geometricModel.SingleCurves.Count;
            heavisideEnrichments = new ThermalInterfaceEnrichment[numCurves];
            for (int c = 0; c < numCurves; ++c)
            {
                heavisideEnrichments[c] = new ThermalInterfaceEnrichment(geometricModel.SingleCurves[c], interfaceResistances[c]);
            }
        }

        public void ApplyEnrichments()
        {
            FindJunctionElements();
            Dictionary<XThermalElement2D, IEnumerable<ThermalInterfaceEnrichment>> cutElements = FindIntersectedElements();
            (Dictionary<XNode, HashSet<ThermalInterfaceEnrichment>> heavisideNodes,
                Dictionary<XNode, HashSet<ThermalJunctionEnrichment>> junctionNodes) = ClassifyNodes(cutElements);

            foreach (XNode node in heavisideNodes.Keys)
            {
                foreach (ThermalInterfaceEnrichment enrichment in heavisideNodes[node])
                {
                    node.EnrichmentItems[enrichment] = enrichment.EvaluateFunctionsAt(node);
                }
            }

            foreach (XNode node in junctionNodes.Keys)
            {
                foreach (ThermalJunctionEnrichment enrichment in junctionNodes[node])
                {
                    node.EnrichmentItems[enrichment] = enrichment.EvaluateFunctionsAt(node);
                }
            }
        }

        //private (Dictionary<XThermalElement2D, IEnumerable<ThermalInterfaceEnrichment>> cutElements,
        //    Dictionary<ThermalJunctionEnrichment, XThermalElement2D> junctions) ClassifyElements()
        //{
        //    int numCurves = geometricModel.SingleCurves.Count;
        //    var cutElements = new Dictionary<XThermalElement2D, IEnumerable<ThermalInterfaceEnrichment>>();
        //    var junctions = new Dictionary<ThermalJunctionEnrichment, XThermalElement2D>();
        //    foreach (XThermalElement2D element in modelElements)
        //    {
        //        var intersectingCurves = new HashSet<int>();
        //        for (int c = 0; c < numCurves; ++c)
        //        {
        //            CurveElementIntersection intersection = geometricModel.SingleCurves[c].IntersectElement(element);
        //            if (intersection.RelativePosition == RelativePositionCurveElement.Intersection)
        //            {
        //                intersectingCurves.Add(c);
        //            }
        //            else if (intersection.RelativePosition == RelativePositionCurveElement.TangentAtSingleNode
        //                || intersection.RelativePosition == RelativePositionCurveElement.TangentAlongElementEdge)
        //            {
        //                Debug.Assert(false);
        //            }
        //        }
        //        if ((intersectingCurves.Count == 1) || (intersectingCurves.Count == 2))
        //        {
        //            cutElements[element] = intersectingCurves.Select(c => heavisideEnrichments[c]);
        //        }
        //        else if (intersectingCurves.Count >= 3) // Junction element
        //        { 
        //            //TODO: What happens if 3 curves intersect the element, without forming a junction?
        //            var junction = new ThermalJunctionEnrichment();
        //            junctions[junction] = element;
        //        }
        //    }
        //    return (cutElements, junctions);
        //}

        private (Dictionary<XNode, HashSet<ThermalInterfaceEnrichment>> heavisideNodes, 
            Dictionary<XNode, HashSet<ThermalJunctionEnrichment>> junctionNodes) ClassifyNodes(
            Dictionary<XThermalElement2D, IEnumerable<ThermalInterfaceEnrichment>> cutElements)
        {
            // Nodes that will be enriched with junction functions
            var junctionNodes = new Dictionary<XNode, HashSet<ThermalJunctionEnrichment>>();
            foreach (var junctionElementPair in junctionEnrichments)
            {
                XThermalElement2D element = junctionElementPair.Key;
                ThermalJunctionEnrichment junction = junctionElementPair.Value;
                foreach (XNode node in element.Nodes)
                {
                    bool isStored = junctionNodes.TryGetValue(node, out HashSet<ThermalJunctionEnrichment> junctionsOfNode);
                    if (!isStored)
                    {
                        junctionsOfNode = new HashSet<ThermalJunctionEnrichment>();
                        junctionNodes[node] = junctionsOfNode;
                    }
                    junctionsOfNode.Add(junction); 
                }
            }

            // Nodes that will be enriched with Heaviside functions
            var heavisideNodes = new Dictionary<XNode, HashSet<ThermalInterfaceEnrichment>>();
            foreach (var elementEnrichmentPair in cutElements)
            {
                XThermalElement2D element = elementEnrichmentPair.Key;
                IEnumerable<ThermalInterfaceEnrichment> heavisideEnrichmentsOfElement = elementEnrichmentPair.Value;
                foreach (XNode node in element.Nodes)
                {
                    if (!junctionNodes.ContainsKey(node))
                    {
                        foreach (ThermalInterfaceEnrichment enrichment in heavisideEnrichmentsOfElement)
                        {
                            bool isStored = heavisideNodes.TryGetValue(node, 
                                out HashSet<ThermalInterfaceEnrichment> enrichmentsOfNode);
                            if (!isStored)
                            {
                                enrichmentsOfNode = new HashSet<ThermalInterfaceEnrichment>();
                                heavisideNodes[node] = enrichmentsOfNode;
                            }
                            enrichmentsOfNode.Add(enrichment);
                        }
                    }
                }
            }

            return (heavisideNodes, junctionNodes);
        }

        private Dictionary<XThermalElement2D, IEnumerable<ThermalInterfaceEnrichment>> FindIntersectedElements()
        {
            int numCurves = geometricModel.SingleCurves.Count;
            var cutElements = new Dictionary<XThermalElement2D, IEnumerable<ThermalInterfaceEnrichment>>();
            foreach (XThermalElement2D element in modelElements)
            {
                if (junctionEnrichments.ContainsKey(element)) continue;

                var intersectingCurves = new HashSet<int>();
                for (int c = 0; c < numCurves; ++c)
                {
                    CurveElementIntersection intersection = geometricModel.SingleCurves[c].IntersectElement(element);
                    if (intersection.RelativePosition == RelativePositionCurveElement.Intersection)
                    {
                        intersectingCurves.Add(c);
                    }
                    else if (intersection.RelativePosition == RelativePositionCurveElement.TangentAtSingleNode
                        || intersection.RelativePosition == RelativePositionCurveElement.TangentAlongElementEdge)
                    {
                        Debug.Assert(false);
                    }
                }
                if (intersectingCurves.Count > 0)
                {
                    cutElements[element] = intersectingCurves.Select(c => heavisideEnrichments[c]);
                }
            }
            return cutElements;
        }

        private void FindJunctionElements()
        {
            junctionEnrichments = new Dictionary<XThermalElement2D, ThermalJunctionEnrichment>();
            foreach (XThermalElement2D element in modelElements)
            {
                var outline = ConvexPolygon2D.CreateUnsafe(element.Nodes);
                foreach (PhaseJunction junction in geometricModel.Junctions)
                {
                    PolygonPointPosition relativePosition = outline.FindRelativePositionOfPoint(junction.Point);
                    if (relativePosition == PolygonPointPosition.Inside)
                    {
                        var junctionEnrichment = new ThermalJunctionEnrichment(junction.Phases);
                        junctionEnrichments[element] = junctionEnrichment;
                        break; // We assume that at most one junction can be inside each element. //TODO: Enforce it.
                    }
                    else if ((relativePosition == PolygonPointPosition.OnEdge)
                        || (relativePosition == PolygonPointPosition.OnVertex))
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }
    }
}
