using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;
using ISAAR.MSolve.XFEM_OLD.Thermal.Curves.MeshInteraction;
using ISAAR.MSolve.XFEM_OLD.Thermal.Elements;
using ISAAR.MSolve.XFEM_OLD.Thermal.Entities;
using static ISAAR.MSolve.Geometry.Shapes.LineSegment2D;

//TODO: Remove diplivations between this and Geometry.Shapes.DirectedSegment2D
namespace ISAAR.MSolve.XFEM_OLD.Thermal.Curves.Explicit.Line
{
    public class LineSegment2D : ICurve2D
    {
        /// <summary>
        /// a is the counter-clockwise angle from the global x axis to the local x axis
        /// </summary>
        private readonly double cosa, sina;

        /// <summary>
        /// The coordinates of the global system's origin in the local system
        /// </summary>
        private readonly double originLocalX, originLocalY;

        /// <summary>
        /// The unit vector that is perpendicular to the segment and faces towards the positive local y axis. 
        /// It is constant for a linear segment, so caching it avoids recalculations.
        /// </summary>
        private readonly double[] normalVector;

        private readonly CartesianPoint start, end;

        public LineSegment2D(CartesianPoint start, CartesianPoint end, double thickness)
        {
            this.Thickness = thickness;
            this.start = start;
            this.end = end;

            double startX = start.X;
            double startY = start.Y;
            double dx = end.X - startX;
            double dy = end.Y - startY;

            Length = Math.Sqrt(dx * dx + dy * dy);
            cosa = dx / Length;
            sina = dy / Length;

            originLocalX = -cosa * startX - sina * startY;
            originLocalY = sina * startX - cosa * startY;

            normalVector = new double[] { -sina, cosa };
        }

        public double Length { get; }

        public double Thickness { get; }

        //TODO: Duplication between this and SimpleLsmClosedCurve2D.FindConformingTriangleVertices()
        public ISet<NaturalPoint> FindConformingTriangleVertices(IXFiniteElement element, CurveElementIntersection intersection)
        {
            // Triangle vertices = union(nodes, intersectionPoints)
            var comparer = new Point2DComparerXMajor<NaturalPoint>(1E-7); //TODO: This should be avoided.
            var triangleVertices = new SortedSet<NaturalPoint>(comparer); //TODO: Better use a HashSet, which needs a hash function for points.

            if (intersection.RelativePosition == RelativePositionCurveElement.Disjoint) return triangleVertices;
            triangleVertices.UnionWith(element.StandardInterpolation.NodalNaturalCoordinates);
            triangleVertices.UnionWith(intersection.IntersectionPoints);

            // Corner case: the element is tangent to the discontinuity. We need to triangulate for plotting the temperature field. //TODO: Really?
            //Debug.Assert(intersection.IntersectionPoints.Length == 2);  

            // Corner case: the curve intersects the element at 2 opposite nodes. In this case also add the middle of their 
            // segment to force the Delauny algorithm to conform to the segment.
            //TODO: I should use constrained Delauny in all cases and conform to the intersection segment.
            if (intersection.IntersectionPoints.Length == 2)
            {
                NaturalPoint p0 = intersection.IntersectionPoints[0];
                NaturalPoint p1 = intersection.IntersectionPoints[1];
                if (element.StandardInterpolation.NodalNaturalCoordinates.Contains(p0)
                    && element.StandardInterpolation.NodalNaturalCoordinates.Contains(p1))
                {
                    triangleVertices.Add(new NaturalPoint(0.5 * (p0.Xi + p1.Xi), 0.5 * (p0.Eta + p1.Eta)));
                }
            }

            return triangleVertices;
        }

        public CurveElementIntersection IntersectElement(IXFiniteElement element)
        {
            //TODO: 1) handle tangent cases, 2) add sketches

            CellType cell = ((IElementType)element).CellType;
            if ((cell != CellType.Tri3) && (cell != CellType.Quad4))
            {
                throw new NotImplementedException("Only works for 1st order elements");
            }

            // Calculate the coordinates of the nodes in the local system of this segment
            //TODO: perhaps I should make sure very small values are replaced by zero. The tolerance should take into account 
            //      the other nodes of the element
            var localNodeCoordinates = new Dictionary<XNode, (double x, double y)>();
            foreach (XNode node in element.Nodes) localNodeCoordinates[node] = (LocalXOf(node), LocalYOf(node));

            // Rough test to efficiently decide for most elements in the mesh 
            if (IsElementFarAway(localNodeCoordinates))
            {
                return new CurveElementIntersection(RelativePositionCurveElement.Disjoint, new NaturalPoint[0], new XNode[0]);
            }

            // Make sure the segment is not tangent to the element
            //if (!IsElementOnTheSameSide(localNodeCoordinates)) throw new NotImplementedException(); //TODO: implement this case

            var intersectionsNatural = new List<NaturalPoint>();
            var intersectionsLocalX = new List<double>(); // Since they are intersections: localY = 0

            // Find nodes that lie on the segment or its extension //TODO: Can reuse data from IsElementFarAway() and IsElementOnTheSameSide
            var nodesOnSegmentOrExtension = new HashSet<XNode>();
            IReadOnlyList<NaturalPoint> nodesNatural = element.StandardInterpolation.NodalNaturalCoordinates;
            for (int n = 0; n < element.Nodes.Count; ++n)
            {
                (double x, double y) = localNodeCoordinates[element.Nodes[n]];
                if (y == 0)
                {
                    nodesOnSegmentOrExtension.Add(element.Nodes[n]);
                    intersectionsNatural.Add(nodesNatural[n]);
                    intersectionsLocalX.Add(x);
                }
            }

            // Iterate each edge of the element to find the intersection points, except if they coincide with nodes
            IReadOnlyList<(XNode node1, XNode node2)> edgesCartesian = element.EdgeNodes;
            IReadOnlyList<(NaturalPoint node1, NaturalPoint node2)> edgesNatural = element.EdgesNodesNatural;
            for (int i = 0; i < edgesCartesian.Count; ++i)
            {
                XNode node1Cartesian = edgesCartesian[i].node1;
                if (nodesOnSegmentOrExtension.Contains(node1Cartesian)) continue; // No need to search for intersections on this edge
                XNode node2Cartesian = edgesCartesian[i].node2;
                if (nodesOnSegmentOrExtension.Contains(node2Cartesian)) continue;

                NaturalPoint node1Natural = edgesNatural[i].node1;
                NaturalPoint node2Natural = edgesNatural[i].node2;
                (double x1, double y1) = localNodeCoordinates[node1Cartesian];
                (double x2, double y2) = localNodeCoordinates[node2Cartesian];

                if (y1 * y2 < 0)
                {
                    // The intersection point between these nodes can be found using linear interpolation
                    double k = -y1 / (y2 - y1);
                    double xIntersection = x1 + k * (x2 - x1); // in the segment's system
                    double xiIntersection = node1Natural.Xi + k * (node2Natural.Xi - node1Natural.Xi); // in the element's system
                    double etaIntersection = node1Natural.Eta + k * (node2Natural.Eta - node1Natural.Eta);
                    intersectionsNatural.Add(new NaturalPoint(xiIntersection, etaIntersection));
                    intersectionsLocalX.Add(xIntersection);

                }
                //else if (y1 * y2 > 0.0) continue; // Edge is not intersected by the segment or its extension
            }
            Debug.Assert(intersectionsNatural.Count == 2); //TODO: Count==1 should also be handled (tangent at node)
            Debug.Assert(intersectionsLocalX[0] != intersectionsLocalX[1]);

            // Make sure the intersection points are on the segment, not its extension
            if (intersectionsLocalX[1] < intersectionsLocalX[0])
            { // sort them so that the left one (in the segment's system) is first
                SwapFirstTwoEntries(intersectionsLocalX);
                SwapFirstTwoEntries(intersectionsNatural);
            }
            int relativePositionLeft = 0; // On the segment
            if (intersectionsLocalX[0] < 0) relativePositionLeft = -1; // left extension 
            else if (intersectionsLocalX[0] > Length) relativePositionLeft = 1; // right extension
            int relativePositionRight = 0;
            if (intersectionsLocalX[1] < 0) relativePositionRight = -1;
            else if (intersectionsLocalX[1] > Length) relativePositionRight = 1;
            if ((relativePositionLeft == 0) && (relativePositionRight == 0))
            {
                // Both intersection points are on the segment, instead of its extension
                return new CurveElementIntersection(RelativePositionCurveElement.Intersection,
                    new NaturalPoint[] { intersectionsNatural[0], intersectionsNatural[1] }, new XNode[0]);
            }
            else if ((relativePositionLeft == 0) && (relativePositionRight == 1))
            {
                // The end vertex of the segment is inside the element
                // Find its natural coordinates using linear interpolation
                double k = (Length - intersectionsLocalX[0]) / (intersectionsLocalX[1] - intersectionsLocalX[0]);
                double xiEnd = intersectionsNatural[0].Xi + k * (intersectionsNatural[1].Xi - intersectionsNatural[0].Xi);
                double etaEnd = intersectionsNatural[0].Eta + k * (intersectionsNatural[1].Eta - intersectionsNatural[0].Eta);
                return new CurveElementIntersection(RelativePositionCurveElement.Intersection,
                    new NaturalPoint[] { intersectionsNatural[0], new NaturalPoint(xiEnd, etaEnd) }, new XNode[0]);
            }
            else if ((relativePositionLeft == -1) && (relativePositionRight == 0))
            {
                // The start vertex of the segment is inside the element
                // Find its natural coordinates using linear interpolation
                double k = (0 - intersectionsLocalX[0]) / (intersectionsLocalX[1] - intersectionsLocalX[0]);
                double xiStart = intersectionsNatural[0].Xi + k * (intersectionsNatural[1].Xi - intersectionsNatural[0].Xi);
                double etaStart = intersectionsNatural[0].Eta + k * (intersectionsNatural[1].Eta - intersectionsNatural[0].Eta);
                return new CurveElementIntersection(RelativePositionCurveElement.Intersection,
                    new NaturalPoint[] { new NaturalPoint(xiStart, etaStart), intersectionsNatural[1] }, new XNode[0]);
            }
            else if ((relativePositionLeft == -1) && (relativePositionRight == 1))
            {
                // The whole segment is inside the element 
                throw new NotImplementedException();
            }
            else if ((intersectionsLocalX[1] <= 0) || (intersectionsLocalX[0] >= Length))
            {
                // Both intersection points line on the left or right extension
                return new CurveElementIntersection(RelativePositionCurveElement.Disjoint, new NaturalPoint[0], new XNode[0]);
            }
            else throw new Exception("Should not have reached this");
        }

        //TODO: Perhaps this can be done more efficiently using the same approach as in LSM and/or the local system of the segment.
        public CurveElementIntersection IntersectElementOLD(IXFiniteElement element)
        {
            CellType cell = ((IElementType)element).CellType;
            if ((cell != CellType.Tri3) && (cell != CellType.Quad4))
            {
                throw new NotImplementedException("Only works for 1st order elements");
            }

            var segment = new Geometry.Shapes.LineSegment2D(start, end);
            IReadOnlyList<(XNode node1, XNode node2)> edgesCartesian = element.EdgeNodes;
            IReadOnlyList<(NaturalPoint node1, NaturalPoint node2)> edgesNatural = element.EdgesNodesNatural;
            var comparer = new Point2DComparerXMajor<CartesianPoint>(1E-7); //TODO: The tolerance should depend on the element size
            var intersectionPointsCartesian = new SortedSet<CartesianPoint>(comparer);
            for (int i = 0; i < edgesCartesian.Count; ++i)
            {
                var edge = new Geometry.Shapes.LineSegment2D(edgesCartesian[i].node1, edgesCartesian[i].node2);
                SegmentSegmentPosition intersection = segment.IntersectionWith(edge, out CartesianPoint intersectionPoint);
                if (intersection == SegmentSegmentPosition.Intersecting) intersectionPointsCartesian.Add(intersectionPoint);
                else if (intersection == SegmentSegmentPosition.Overlapping)
                {
                    // This line segment is tangent to the element, but we do not know if both nodes lie on it.
                    var tangentPoints = new List<XNode>(2);
                    var tangentPointsNatural = new List<NaturalPoint>(2);
                    double x1 = LocalXOf(edgesCartesian[i].node1);
                    if ((x1 >= 0) && (x1 <= Length))
                    {
                        tangentPoints.Add(edgesCartesian[i].node1);
                        tangentPointsNatural.Add(edgesNatural[i].node1);
                    }
                    double x2 = LocalXOf(edgesCartesian[i].node2);
                    if ((x2 >= 0) && (x2 <= Length))
                    {
                        tangentPoints.Add(edgesCartesian[i].node2);
                        tangentPointsNatural.Add(edgesNatural[i].node2);
                    }
                    if (tangentPoints.Count == 1)
                    {
                        return new CurveElementIntersection(RelativePositionCurveElement.TangentAtSingleNode,
                            tangentPointsNatural.ToArray(), tangentPoints.ToArray());
                    }
                    else if (tangentPoints.Count == 2)
                    {
                        return new CurveElementIntersection(RelativePositionCurveElement.TangentAlongElementEdge,
                            tangentPointsNatural.ToArray(), tangentPoints.ToArray());
                    }
                    else throw new NotImplementedException("This should not have happened");
                }
            }

            if (intersectionPointsCartesian.Count > 0)
            {
                // Also check if the vertices of the segment are inside the element
                var polygon = ConvexPolygon2D.CreateUnsafe(element.Nodes);
                if (polygon.FindRelativePositionOfPoint(start) != PolygonPointPosition.Outside)
                {
                    intersectionPointsCartesian.Add(start);
                }
                if (polygon.FindRelativePositionOfPoint(end) != PolygonPointPosition.Outside)
                {
                    intersectionPointsCartesian.Add(end);
                }
                Debug.Assert(intersectionPointsCartesian.Count == 2);

                // Map the intersection points to the natural system of the element before returning
                FEM.Interpolation.Inverse.IInverseInterpolation2D inverseMapping = 
                    element.StandardInterpolation.CreateInverseMappingFor(element.Nodes);
                NaturalPoint[] intersectionPointsNatural = intersectionPointsCartesian.Select(
                    p => inverseMapping.TransformPointCartesianToNatural(p)).ToArray();
                return new CurveElementIntersection(RelativePositionCurveElement.Intersection, 
                    intersectionPointsNatural, new XNode[0]);
            }
            else
            {
                // The element does not interact with this line segment 
                return new CurveElementIntersection(RelativePositionCurveElement.Disjoint, new NaturalPoint[0], new XNode[0]);
            }
        }

        public double SignedDistanceOf(XNode node) => LocalYOf(node);

        public double SignedDistanceOf(IXFiniteElement element, double[] shapeFunctionsAtNaturalPoint)
        {
            double x = 0.0, y = 0.0;
            for (int n = 0; n < shapeFunctionsAtNaturalPoint.Length; ++n)
            {
                INode node = element.Nodes[n];
                double N = shapeFunctionsAtNaturalPoint[n];
                x += N * node.X;
                y += N * node.Y;
            }
            return -sina * x + cosa * y + originLocalY;
        }

        /// <summary>
        /// Even if false is returned, the element and segment can still be disjoint. However this method serves to quickly 
        /// process most of the elements in the mesh.
        /// </summary>
        /// <param name="localNodeCoordinates"></param>
        /// <returns></returns>
        private bool IsElementFarAway(Dictionary<XNode, (double x, double y)> localNodeCoordinates)
        {
            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;
            foreach ((double x, double y) in localNodeCoordinates.Values)
            {
                if (x < minX) minX = x;
                else if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                else if (y > maxY) maxY = y;
            }


            if (minY * maxY > 0.0) return true; // Not intersected by the segment or its extension
            else if ((minX > Length) || (maxX < 0)) return true; // Intersected the segment's extension
            else return false;
        }

        private bool IsElementOnTheSameSide(Dictionary<XNode, (double x, double y)> localNodeCoordinates)
        {
            //TODO: Can reuse data from IsElementFarAway()
            int numNodesWithPositiveY = 0;
            int numNodesWithNegativeY = 0;
            foreach ((double x, double y) in localNodeCoordinates.Values)
            {
                if (y > 0) ++numNodesWithPositiveY;
                else if (y < 0) ++numNodesWithNegativeY;
            }

            if ((numNodesWithPositiveY > 0) && (numNodesWithNegativeY > 0)) return false;
            else return true; 
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double LocalXOf(CartesianPoint point) => cosa * point.X + sina * point.Y + originLocalX;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double LocalYOf(CartesianPoint point) => -sina * point.X + cosa * point.Y + originLocalY;

        private void SwapFirstTwoEntries<T>(List<T> list)
        {
            T temp = list[0];
            list[0] = list[1];
            list[1] = temp;
        }
    }
}
