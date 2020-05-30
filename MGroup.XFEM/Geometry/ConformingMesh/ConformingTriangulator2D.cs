using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Geometry.Tolerances;
using MIConvexHull;

//TODO: Allow the option to specify the minimum triangle area.
namespace MGroup.XFEM.Geometry.ConformingMesh
{
    public class ConformingTriangulator2D
    {
        public ElementSubtriangle2D[] FindConformingMesh(IXFiniteElement element, 
            IEnumerable<IElementCurveIntersection2D> intersections, IMeshTolerance meshTolerance)
        {
            // Store the nodes and all intersection points in a set
            double tol = meshTolerance.CalcTolerance(element);
            var comparer = new Point2DComparer<NaturalPoint>(tol);
            var triangleVertices = new SortedSet<NaturalPoint>(comparer);
            triangleVertices.UnionWith(element.InterpolationStandard.NodalNaturalCoordinates);
            
            // Also store just the nodes elsewhere
            var nodes = new SortedSet<NaturalPoint>(comparer);
            nodes.UnionWith(element.InterpolationStandard.NodalNaturalCoordinates);

            foreach (IElementCurveIntersection2D intersection in intersections)
            {
                // If the curve does not intersect this element (e.g. it conforms to the element edge), 
                // there is no need to triangulate
                if (intersection.RelativePosition != RelativePositionCurveElement.Intersecting) continue;

                IList<NaturalPoint> newVertices = intersection.GetPointsForTriangulation();
                int countBeforeInsertion = triangleVertices.Count;
                triangleVertices.UnionWith(newVertices);

                if (triangleVertices.Count > countBeforeInsertion)
                {
                    // Corner case: the curve intersects the element at 2 opposite nodes. In this case also add the middle of their 
                    // segment to force the Delauny algorithm to conform to the segment.
                    //TODO: I should use constrained Delauny in all cases and conform to the intersection segment.
                    NaturalPoint p0 = newVertices[0];
                    NaturalPoint p1 = newVertices[1];
                    if (nodes.Contains(p0) && nodes.Contains(p1))
                    {
                        triangleVertices.Add(new NaturalPoint(05 * (p0.Xi + p1.Xi), 0.5 * (p0.Eta + p1.Eta)));
                    }
                }
            }

            FindIntersectionsOfIntersections();

            // Create Delauny triangulation
            ElementSubtriangle2D[] subtriangles = Triangulate(triangleVertices);
            return subtriangles;
        }

        //TODO: Use a wrapper for interoperability with the MIConvexHull library
        private ElementSubtriangle2D[] Triangulate(SortedSet<NaturalPoint> trianglePoints)
        {
            // Gather the vertices
            var vertices = new List<double[]>(trianglePoints.Count);
            foreach (NaturalPoint point in trianglePoints)
            {
                vertices.Add(new double[] { point.Xi, point.Eta });
            }

            // Call 3rd-party mesh generator
            var triangleCells = Triangulation.CreateDelaunay(vertices).Cells.ToArray();

            // Repackage the triangle cells
            var subtriangles = new ElementSubtriangle2D[triangleCells.Length];
            for (int t = 0; t < subtriangles.Length; ++t)
            {
                DefaultVertex[] verticesOfTriangle = triangleCells[t].Vertices;
                Debug.Assert(verticesOfTriangle.Length == 3);
                var pointsOfTriangle = new NaturalPoint[3];
                for (int v = 0; v < 3; ++v)
                {
                    pointsOfTriangle[v] = new NaturalPoint(verticesOfTriangle[v].Position);
                }
                subtriangles[t] = new ElementSubtriangle2D(pointsOfTriangle);
            }
            return subtriangles;
        }

        private void FindIntersectionsOfIntersections()
        {
            // No need for now. We assume that level sets do not intersect.
            throw new NotImplementedException();
        }
    }
}
