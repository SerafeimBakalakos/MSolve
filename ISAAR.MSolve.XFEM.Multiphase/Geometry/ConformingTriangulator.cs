using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Triangulation;
using ISAAR.MSolve.XFEM.Multiphase.Elements;

//TODO: Allow the option to specify the minimum triangle area.
namespace ISAAR.MSolve.XFEM.Multiphase.Geometry
{
    public class ConformingTriangulator
    {
        public ElementSubtriangle[] FindConformingMesh(IXFiniteElement element, 
            IEnumerable<CurveElementIntersection> intersections, IMeshTolerance meshTolerance)
        {
            double tol = meshTolerance.CalcTolerance(element);
            var comparer = new Point2DComparerXMajor<NaturalPoint>(tol); //TODO: This should be avoided.
            var triangleVertices = new SortedSet<NaturalPoint>(comparer); //TODO: Better use a HashSet, which needs a hash function for points.
            triangleVertices.UnionWith(element.InterpolationStandard.NodalNaturalCoordinates);
            var nodes = new SortedSet<NaturalPoint>(comparer);
            nodes.UnionWith(element.InterpolationStandard.NodalNaturalCoordinates);

            foreach (CurveElementIntersection intersection in intersections)
            {
                // If the boundary goes through an element side, there is no need to triangulate
                if (intersection.RelativePosition != RelativePositionCurveElement.Intersection) continue;

                // Otherwise keep the intersection points
                triangleVertices.UnionWith(intersection.IntersectionPoints);

                // Corner case: the curve intersects the element at 2 opposite nodes. In this case also add the middle of their 
                // segment to force the Delauny algorithm to conform to the segment.
                //TODO: I should use constrained Delauny in all cases and conform to the intersection segment.
                NaturalPoint p0 = intersection.IntersectionPoints[0];
                NaturalPoint p1 = intersection.IntersectionPoints[1];
                if (nodes.Contains(p0) && nodes.Contains(p1))
                {
                    triangleVertices.Add(new NaturalPoint(05 * (p0.Xi + p1.Xi), 0.5 * (p0.Eta + p1.Eta)));
                }
            }

            // Create Delauny triangulation
            var triangulator = new Triangulator2D<NaturalPoint>((x1, x2) => new NaturalPoint(x1, x2));
            List<Triangle2D<NaturalPoint>> triangles = triangulator.CreateMesh(triangleVertices);
           
            // Repackage the triangles
            var subtriangles = new ElementSubtriangle[triangles.Count];
            for (int i = 0; i < subtriangles.Length; ++i) subtriangles[i] = new ElementSubtriangle(triangles[i].Vertices);
            return subtriangles;
        }
    }
}
