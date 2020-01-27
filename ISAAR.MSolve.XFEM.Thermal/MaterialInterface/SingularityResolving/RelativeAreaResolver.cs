using System;
using System.Collections.Generic;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;
using ISAAR.MSolve.XFEM.ThermalOLD.Elements;
using ISAAR.MSolve.XFEM.ThermalOLD.Entities;
using ISAAR.MSolve.XFEM.ThermalOLD.Curves;
using ISAAR.MSolve.XFEM.ThermalOLD.Curves.MeshInteraction;

namespace ISAAR.MSolve.XFEM.ThermalOLD.MaterialInterface.SingularityResolving
{
    public class RelativeAreaResolver : IHeavisideSingularityResolver
    {
        private readonly GeometricModel2D geometricModel;
        private readonly double relativeAreaTolerance;

        public RelativeAreaResolver(GeometricModel2D geometricModel, double relativeAreaTolerance = 1E-4)
        {
            this.geometricModel = geometricModel;
            this.relativeAreaTolerance = relativeAreaTolerance;
        }

        /// <summary>
        /// Given a set of Heaviside enriched nodes, find which of them must not be enriched, in order to avoid the global
        /// stiffness matrix being singular.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="heavisideNodes">They will not be altered.</param>
        /// <returns></returns>
        public HashSet<XNode> FindHeavisideNodesToRemove(Curves.ICurve2D curve, IEnumerable<XNode> heavisideNodes)
        {
            var processedElements = new Dictionary<IXFiniteElement, (double elementPosArea, double elementNegArea)>();
            var nodesToRemove = new HashSet<XNode>();
            foreach (XNode node in heavisideNodes)
            {
                double nodePositiveArea = 0.0;
                double nodeNegativeArea = 0.0;

                foreach (IXFiniteElement element in node.ElementsDictionary.Values)
                {
                    bool alreadyProcessed = processedElements.TryGetValue(element, out (double pos, double neg) elementAreas);
                    if (!alreadyProcessed)
                    {
                        elementAreas = FindSignedAreasOfElement(curve, element);
                        processedElements[element] = elementAreas;
                    }
                    nodePositiveArea += elementAreas.pos;
                    nodeNegativeArea += elementAreas.neg;
                }

                if (curve.SignedDistanceOf(node) >= 0.0)
                {
                    double negativeAreaRatio = nodeNegativeArea / (nodePositiveArea + nodeNegativeArea);
                    if (negativeAreaRatio < relativeAreaTolerance) nodesToRemove.Add(node);
                }
                else
                {
                    double positiveAreaRatio = nodePositiveArea / (nodePositiveArea + nodeNegativeArea);
                    if (positiveAreaRatio < relativeAreaTolerance) nodesToRemove.Add(node);
                }
            }

            return nodesToRemove;
        }

        private NaturalPoint FindElementCentroid2D(IXFiniteElement element)
        {
            double centroidXi = 0.0, centroidEta = 0.0;
            IReadOnlyList<NaturalPoint> nodes = element.StandardInterpolation.NodalNaturalCoordinates;
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
        private (double positiveArea, double negativeArea) FindSignedAreasOfElement(Curves.ICurve2D curve, IXFiniteElement element)
        {
            double positiveArea = 0.0;
            double negativeArea = 0.0;

            // Split the element into subtriangles
            bool success = geometricModel.TryConformingTriangulation(element, out IReadOnlyList<ElementSubtriangle> subtriangles);
            if (success)
            {
                foreach (ElementSubtriangle triangle in subtriangles)
                {
                    // Calculate their areas and on which side they lie, based on their centroids
                    double area = triangle.CalcAreaCartesian(element);
                    NaturalPoint centroid = triangle.FindCentroid();
                    double[] shapeFunctions = element.StandardInterpolation.EvaluateFunctionsAt(centroid);
                    double signedDistance = curve.SignedDistanceOf(element, shapeFunctions);
                    if (signedDistance > 0) positiveArea += area;
                    else if (signedDistance < 0) negativeArea += area;
                    else throw new Exception("Found subtriangle whose centroid lies on a discontinuity");
                }
            }
            else
            {
                // Calculate the are of the whole element and on which side it lies, based on its centroid
                double area = ConvexPolygon2D.CreateUnsafe(element.Nodes).ComputeArea(); //TODO: This only works for 1st order elements.
                NaturalPoint centroid = FindElementCentroid2D(element);
                double[] shapeFunctions = element.StandardInterpolation.EvaluateFunctionsAt(centroid);
                double signedDistance = curve.SignedDistanceOf(element, shapeFunctions);
                if (signedDistance > 0) positiveArea += area;
                else if (signedDistance < 0) negativeArea += area;
                else throw new Exception("Found element not intersected by the discontinuity whose centroid lies on it");
            }

            return (positiveArea, negativeArea);
        }
    }
}
