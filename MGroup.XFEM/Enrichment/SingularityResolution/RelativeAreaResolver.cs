using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Geometry.Primitives;

//TODO: Finding the phases of the conforming subcells must be performed by, stored in and accessed from the element. Or 
//      ElementSubtriangle should store that data.
namespace MGroup.XFEM.Enrichment.SingularityResolution
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
        public HashSet<XNode> FindStepEnrichedNodesToRemove(IEnumerable<XNode> stepNodes, StepEnrichment enrichment)
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

                if (node.Phase.ID == phase1ID)
                {
                    double area2Ratio = nodeArea2 / (nodeArea1 + nodeArea2);
                    if (area2Ratio < relativeAreaTolerance) nodesToRemove.Add(node);
                }
                else
                {
                    Debug.Assert(node.Phase.ID == phase2ID, "This node does not belong to any of the 2 phases of its enrichment");
                    double area1Ratio = nodeArea1 / (nodeArea1 + nodeArea2);
                    if (area1Ratio < relativeAreaTolerance) nodesToRemove.Add(node);
                }
            }

            return nodesToRemove;
        }

        // TODO: I should really cache these somehow, so that they can be accessible from the crack object. They are used at various points.
        private (double totalArea1, double totalArea2) FindSignedAreasOfElement(IXFiniteElement element, 
            StepEnrichment enrichment)
        {
            IPhase phase1 = enrichment.Phases[0];
            IPhase phase2 = enrichment.Phases[1];
            double totalBulkSize1 = 0.0;
            double totalBulkSize2 = 0.0;

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
                IElementSubcell[] subcells = element.ConformingSubcells;

                // We assume that these triangles will be in one of the 2 phases of the boundary. This does not hold if the 
                // element is intersected by 2 or more boundaries. However in that case, its nodes would be enriched with
                // junction function, not step function.
                foreach (IElementSubcell subcell in subcells)
                {
                    // Calculate their areas and on which side they lie, based on their centroids

                    // This was for explicit geometries
                    //(CartesianPoint centroid, double area) = triangle.FindCentroidAndAreaCartesian(element2D);
                    //IPhase phase = GeometricModel.FindPhaseAt(centroid, element);

                    NaturalPoint centroidNatural = subcell.FindCentroidNatural();
                    (double[] centroidCartesian, double bulkSize) = subcell.FindCentroidAndBulkSizeCartesian(element);
                    XPoint centroid = new XPoint();
                    centroid.Coordinates[CoordinateSystem.ElementNatural] = centroidNatural.Coordinates;
                    element.FindPhaseAt(centroid);

                    if (centroid.Phase == phase1) totalBulkSize1 += bulkSize;
                    else
                    {
                        Debug.Assert(centroid.Phase == phase2, "Found subtriangle whose centroid lies on a discontinuity");
                        totalBulkSize2 += bulkSize;
                    }
                }
            }
            else
            {
                // Calculate the area/volume of the whole element and on which side it lies
                double bulkSize = element.CalcBulkSize();
                
                if (element.Phases.Count == 1)
                {
                    IPhase phase = element.Phases.First();
                    if (phase == phase1) totalBulkSize1 += bulkSize;
                    else
                    {
                        Debug.Assert(phase == phase2, "Found element without a surrounding phase");
                        totalBulkSize2 += bulkSize;
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
                        if (node.Phase == phase1)
                        {
                            phase = node.Phase;
                            break;
                        }
                        else if (node.Phase == phase2)
                        {
                            phase = node.Phase;
                            break;
                        }
                    }

                    Debug.Assert(phase != null);
                    if (phase == phase1) totalBulkSize1 += bulkSize;
                    else totalBulkSize2 += bulkSize;
                }

            }

            return (totalBulkSize1, totalBulkSize2);
        }
    }
}
