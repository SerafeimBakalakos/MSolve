using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Entities;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Enrichment.SingularityResolution
{
    public class RelativeAreaResolver : ISingularityResolver
    {
        private readonly GeometricModel geometricModel;
        private readonly double relativeAreaTolerance;

        public RelativeAreaResolver(GeometricModel geometricModel, double relativeAreaTolerance = 1E-4)
        {
            this.geometricModel = geometricModel;
            this.relativeAreaTolerance = relativeAreaTolerance;
        }

        /// <summary>
        /// Given a set of step enriched nodes, find which of them must not be enriched, in order to avoid the global
        /// stiffness matrix being singular.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="stepNodes">They will not be altered.</param>
        public HashSet<XNode> FindStepEnrichedNodesToRemove(IEnumerable<XNode> stepNodes, StepEnrichmentOLD enrichment)
        {
            int phase1ID = enrichment.Phases[0].ID;
            int phase2ID = enrichment.Phases[1].ID;

            var processedElements = new Dictionary<IXFiniteElement, (double elementArea1, double elementArea2)>();
            var nodesToRemove = new HashSet<XNode>();
            foreach (XNode node in stepNodes)
            {
                double nodeArea1 = 0.0;
                double nodeArea2 = 0.0;

                foreach (IXFiniteElement element in node.ElementsDictionary.Values)
                {
                    bool alreadyProcessed = processedElements.TryGetValue(element, out (double A1, double A2) elementAreas);
                    if (!alreadyProcessed)
                    {
                        elementAreas = FindSignedAreasOfElement(element, enrichment);
                        processedElements[element] = elementAreas;
                    }
                    nodeArea1 += elementAreas.A1;
                    nodeArea2 += elementAreas.A2;
                }

                IPhase nodePhase = enrichment.FindPhaseAt(node);
                if (nodePhase.ID == phase1ID)
                {
                    double area2Ratio = nodeArea2 / (nodeArea1 + nodeArea2);
                    if (area2Ratio < relativeAreaTolerance) nodesToRemove.Add(node);
                }
                else
                {
                    Debug.Assert(nodePhase.ID == phase2ID, "This node does not belong to any of the 2 phases of its enrichment");
                    double area1Ratio = nodeArea1 / (nodeArea1 + nodeArea2);
                    if (area1Ratio < relativeAreaTolerance) nodesToRemove.Add(node);
                }
            }

            return nodesToRemove;
        }

        private NaturalPoint FindElementCentroid2D(IXFiniteElement element)
        {
            double centroidXi = 0.0, centroidEta = 0.0;
            IReadOnlyList<NaturalPoint> nodes = element.InterpolationStandard.NodalNaturalCoordinates;
            foreach (NaturalPoint node in nodes)
            {
                centroidXi += node.Xi;
                centroidEta += node.Eta;
            }
            centroidXi /= nodes.Count;
            centroidEta /= nodes.Count;
            return new NaturalPoint(centroidXi, centroidEta);
        }

        // TODO: I should really cache these somehow, so that they can be accessible from the crack object. They are used at various points.
        private (double totalArea1, double totalArea2) FindSignedAreasOfElement(IXFiniteElement element, 
            StepEnrichmentOLD enrichment)
        {
            IPhase phase1 = enrichment.Phases[0];
            IPhase phase2 = enrichment.Phases[1];
            double totalArea1 = 0.0;
            double totalArea2 = 0.0;

            // Determine if the element is intersected by the boundary of this step enrichment
            bool isIntersectedByBoundary = false;
            foreach (PhaseBoundary boundary in element.PhaseIntersections.Keys)
            {
                if (enrichment.IsAppliedDueTo(boundary))
                {
                    isIntersectedByBoundary = true;
                    break;
                }
            }

            if (isIntersectedByBoundary)
            {
                IReadOnlyList<ElementSubtriangle> subtriangles = geometricModel.GetConformingTriangulationOf(element);

                // We assume that these triangles will be in one of the 2 phases of the boundary. This does not hold if the 
                // element is intersected by 2 or more boundaries. However in that case, its nodes would be enriched with
                // junction function, not step function.
                foreach (ElementSubtriangle triangle in subtriangles)
                {
                    // Calculate their areas and on which side they lie, based on their centroids
                    (CartesianPoint centroid, double area) = triangle.FindCentroidAndAreaCartesian(element);
                    IPhase phase = GeometricModel.FindPhaseAt(centroid, element);
                    if (phase == phase1) totalArea1 += area;
                    else
                    {
                        Debug.Assert(phase == phase2, "Found subtriangle whose centroid lies on a discontinuity");
                        totalArea2 += area;
                    }
                }
            }
            else
            {
                // Calculate the are of the whole element and on which side it lies
                double area = ConvexPolygon2D.CreateUnsafe(element.Nodes).ComputeArea(); //TODO: This only works for 1st order elements.
                
                if (element.Phases.Count == 1)
                {
                    IPhase phase = element.Phases.First();
                    if (phase == phase1) totalArea1 += area;
                    else
                    {
                        Debug.Assert(phase == phase2, "Found element without a surrounding phase");
                        totalArea2 += area;
                    }
                }
                else 
                {
                    // The element might contain more than one phases that are not seperated by the boundary of the step 
                    // enrichment. In this case, at least one of its nodes will be in one of the 2 phases. Find that node and 
                    // phase.
                    IPhase phase = null;
                    foreach (XNode node in element.Nodes)
                    {
                        if (node.SurroundingPhase == phase1)
                        {
                            phase = node.SurroundingPhase;
                            break;
                        }
                        else if (node.SurroundingPhase == phase2)
                        {
                            phase = node.SurroundingPhase;
                            break;
                        }
                    }

                    Debug.Assert(phase != null);
                    if (phase == phase1) totalArea1 += area;
                    else totalArea2 += area;
                }

            }

            return (totalArea1, totalArea2);
        }
    }
}
