using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Geometry.Tolerances;
using MIConvexHull;

//TODO: Allow the option to specify the minimum triangle area.
namespace MGroup.XFEM.Geometry.ConformingMesh
{
    /// <summary>
    /// Does not work correctly if an element is intersected by more than one curves, which also intersect each other.
    /// </summary>
    public class ConformingTriangulator2D
    {
        public ElementSubtriangle2D[] FindConformingMesh(IXFiniteElement element, 
            IEnumerable<IElementCurveIntersection2D> intersections, IMeshTolerance meshTolerance)
        {
            // Store the nodes and all intersection points in a set
            double tol = meshTolerance.CalcTolerance(element);
            var comparer = new Point2DComparer<NaturalPoint>(tol);
            var nodes = new SortedSet<NaturalPoint>(comparer);
            nodes.UnionWith(element.Interpolation2D.NodalNaturalCoordinates);

            // Store the nodes and all intersection points in a different set
            var triangleVertices = new SortedSet<NaturalPoint>(comparer);
            triangleVertices.UnionWith(nodes);

            // Add intersection points from each curve-element intersection object.
            foreach (IElementCurveIntersection2D intersection in intersections)
            {
                // If the curve does not intersect this element (e.g. it conforms to the element edge), 
                // there is no need to take into account for triangulation
                if (intersection.RelativePosition != RelativePositionCurveElement.Intersecting) continue;

                IList<NaturalPoint> newVertices = intersection.GetPointsForTriangulation();
                int countBeforeInsertion = triangleVertices.Count;
                triangleVertices.UnionWith(newVertices);

                if (triangleVertices.Count == countBeforeInsertion)
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

            var triangulator = new MIConvexHullTriangulator2D();
            triangulator.MinTriangleArea = tol * element.CalcAreaOrVolume();
            IList<Triangle2D> delaunyTriangles = triangulator.CreateMesh(triangleVertices);
            var subtriangles = new ElementSubtriangle2D[delaunyTriangles.Count];
            for (int t = 0; t < delaunyTriangles.Count; ++t)
            {
                subtriangles[t] = new ElementSubtriangle2D(delaunyTriangles[t]);
            }
            return subtriangles;
        }
    }
}
