using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry
{
    public class LineSegment2D
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

        public LineSegment2D(CartesianPoint start, CartesianPoint end)
        {
            this.Start = start;
            this.End = end;

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

        public CartesianPoint End { get; }

        public double Length { get; }

        public CartesianPoint Start { get; }

        public CurveElementIntersection IntersectElement(IXFiniteElement element, IMeshTolerance meshTolerance)
        {
            Check1stOrderElement(element);

            double tol = meshTolerance.CalcTolerance(element);

            // Calculate the coordinates of the nodes in the local system of this segment
            //TODO: perhaps I should make sure very small values are replaced by zero. The tolerance should take into account 
            //      the other nodes of the element
            var localNodeCoordinates = new Dictionary<XNode, (double r, double s)>();
            foreach (XNode node in element.Nodes)
            {
                double r = cosa * node.X + sina * node.Y + originLocalX;
                double s = -sina * node.X + cosa * node.Y + originLocalY;
                localNodeCoordinates[node] = (r, s);
            }

            // Rough test to efficiently decide for most elements in the mesh 
            if (IsElementFarAway(localNodeCoordinates))
            {
                return new CurveElementIntersection(RelativePositionCurveElement.Disjoint, new NaturalPoint[0]);
            }

            // Count nodes with s = 0, < 0 or > 0
            int numZeroNodes = 0, numPosNodes = 0, numNegNodes = 0;
            foreach ((double r, double s) in localNodeCoordinates.Values)
            {
                if (Math.Abs(s) < tol) ++numZeroNodes;
                else if (s < -tol) ++numNegNodes;
                else ++numPosNodes;
            }

            // Find the relative position of the line and the element
            if ((numPosNodes > 0) && (numNegNodes > 0)) // element is intersected by the line segment or its extension 
            {
                // Find intersection points between the line and each edge, sort them in ascending order of local segment 
                // coordinate r and remove duplicate entries (if an intersection point is a node, it is counted twice)
                List<(double r, NaturalPoint P)> allIntersections = FindElementEdgesIntersections(element, localNodeCoordinates);
                var comparer = new LocalRComparer(tol);
                var uniqueLineElementIntersections = new SortedDictionary<double, NaturalPoint>(comparer);
                foreach ((double r, NaturalPoint P) in allIntersections) uniqueLineElementIntersections[r] = P;
                Debug.Assert(uniqueLineElementIntersections.Count == 2);

                // Find the intersection points between this line segment AB and the segment P1P2 defined by the 2 intersection 
                // points between the line and the element.
                NaturalPoint[] intersectionPoints = FindCollinearSegmentIntersections(uniqueLineElementIntersections);
                if (intersectionPoints.Length < 2)
                {
                    return new CurveElementIntersection(RelativePositionCurveElement.Disjoint, intersectionPoints);
                }
                else return new CurveElementIntersection(RelativePositionCurveElement.Intersection, intersectionPoints);
            }
            else if (numZeroNodes == 2) // the line segment or its extension goes through an element side
            {
                // Find the 2 nodes N1, N2 that lie on the whole line
                var nodesOnLine = new SortedDictionary<double, NaturalPoint>();
                for (int n = 0; n < element.Nodes.Count; ++n)
                {
                    (double r, double s) = localNodeCoordinates[element.Nodes[n]];
                    if (Math.Abs(s) < tol)
                    {
                        nodesOnLine[r] = element.InterpolationStandard.NodalNaturalCoordinates[n];
                    }
                }

                // Find the intersection points between this line segment AB and the segment N1N2 defined by the 2 nodes that 
                // lie on the line. 
                NaturalPoint[] intersectionPoints = FindCollinearSegmentIntersections(nodesOnLine);
                if (intersectionPoints.Length < 2)
                {
                    return new CurveElementIntersection(RelativePositionCurveElement.Disjoint, intersectionPoints);
                }
                else return new CurveElementIntersection(RelativePositionCurveElement.Tangent, intersectionPoints);
            }
            else if (numZeroNodes == 1) // the line segment or its extension goes through only 1 node of the element
            {
                NaturalPoint[] contactPoints = FindSingleNodeOnSegment(element, localNodeCoordinates, tol);
                return new CurveElementIntersection(RelativePositionCurveElement.Disjoint, contactPoints);
            }
            else throw new NotImplementedException("Unpredicted case");
        }

        public double SignedDistanceOf(CartesianPoint point) => -sina * point.X + cosa * point.Y + originLocalY;
        public double SignedDistanceOf(XNode node) => -sina * node.X + cosa * node.Y + originLocalY;

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

        [Conditional("DEBUG")]
        private void Check1stOrderElement(IXFiniteElement element)
        {
            CellType cell = ((IElementType)element).CellType;
            if ((cell != CellType.Tri3) && (cell != CellType.Quad4))
            {
                throw new NotImplementedException("Only works for 1st order elements");
            }
        }

        private NaturalPoint[] FindCollinearSegmentIntersections(SortedDictionary<double, NaturalPoint> lineElementIntersections)
        {
            // Access these 2 intersection points 
            var firstPair = lineElementIntersections.First();
            double r1 = firstPair.Key;
            NaturalPoint P1 = firstPair.Value;
            var secondPair = lineElementIntersections.Last();
            double r2 = secondPair.Key;
            NaturalPoint P2 = secondPair.Value;

            //TODO: Use tolerance for some of these
            if (r1 < 0.0)
            {
                if (r2 < 0.0) return new NaturalPoint[0];
                else if (r2 == 0.0) return new NaturalPoint[] { P2 };
                else if (r2 <= Length)
                {
                    NaturalPoint A = TransformLocalToNatural(0.0, P1, r1, P2, r2);
                    return new NaturalPoint[] { A, P2 };
                }
                else
                {
                    NaturalPoint A = TransformLocalToNatural(0.0, P1, r1, P2, r2);
                    NaturalPoint B = TransformLocalToNatural(Length, P1, r1, P2, r2);
                    return new NaturalPoint[] { A, B };
                }
            }
            else if (r1 < Length)
            {
                Debug.Assert(r2 > 0.0);
                if (r2 <= Length) return new NaturalPoint[] { P1, P2 };
                else
                {
                    NaturalPoint B = TransformLocalToNatural(Length, P1, r1, P2, r2);
                    return new NaturalPoint[] { P1, B };
                }
            }
            else if (r1 == Length)
            {
                Debug.Assert(r2 > 0.0);
                return new NaturalPoint[] { P1 };
            }
            else // start > L
            {
                Debug.Assert(r2 > 0.0);
                return new NaturalPoint[0];
            }

            #region old code
            //TODO: Use tolerance for some of these
            //    if (r1 < 0.0)
            //    {
            //        if (r2 < 0.0) return (false, new double[0]);
            //        else if (r2 == 0.0) return (false, new double[] { r2 });
            //        else if (r2 <= Length) return (true, new double[] { 0.0, r2 });
            //        else return (true, new double[] { 0.0, Length });
            //    }
            //    else if (r1 < Length)
            //    {
            //        Debug.Assert(r2 > 0.0);
            //        if (r2 <= Length) return (true, new double[] { r1, r2 });
            //        else return (true, new double[] { r1, Length });
            //    }
            //    else if (r1 == Length)
            //    {
            //        Debug.Assert(r2 > 0.0);
            //        return (false, new double[] { r1 });
            //    }
            //    else // start > L
            //    {
            //        Debug.Assert(r2 > 0.0);
            //        return (false, new double[0]);
            //    }
            #endregion
        }

        private List<(double r, NaturalPoint P)> FindElementEdgesIntersections(IXFiniteElement element, 
            Dictionary<XNode, (double r, double s)> localNodeCoordinates)
        {
            var intersectionPoints = new List<(double, NaturalPoint)>(4); // at most 2 nodes will be added twice
            IReadOnlyList<(XNode node1, XNode node2)> edgesCartesian = element.EdgeNodes;
            IReadOnlyList<(NaturalPoint node1, NaturalPoint node2)> edgesNatural = element.EdgesNodesNatural;

            // Iterate each edge of the element to find the intersection points (which may coincide with nodes)
            for (int i = 0; i < edgesCartesian.Count; ++i)
            {
                // Check if the edge is intersected by the line
                (XNode node1, XNode node2) = edgesCartesian[i];
                (double r1, double s1) = localNodeCoordinates[node1];
                (double r2, double s2) = localNodeCoordinates[node2];
                if (s1 * s2 <= 0)
                {
                    (NaturalPoint N1, NaturalPoint N2) = edgesNatural[i];
                    double k = -s1 / (s2 - s1);
                    double r = r1 + k * (r2 - r1);
                    double xi = N1.Xi + k * (N2.Xi - N1.Xi);
                    double eta = N1.Eta + k * (N2.Eta - N1.Eta);
                    intersectionPoints.Add((r, new NaturalPoint(xi, eta)));

                    #region sorted nodes. There is no need
                    //// Sort the nodes that lie opposite of the line, such that ri < rj
                    //XNode Ni, Nj;
                    //NaturalPoint Mi, Mj;
                    //double ri, si, rj, sj;
                    //if (r1 < r2)
                    //{
                    //    Ni = node1; Mi = edgeNatural.Item1; ri = r1; si = s1;
                    //    Nj = node2; Mj = edgeNatural.Item2; rj = r2; sj = s2;
                    //}
                    //else
                    //{
                    //    Ni = node2; Mi = edgeNatural.Item2; ri = r2; si = s2;
                    //    Nj = node1; Mj = edgeNatural.Item1; rj = r1; sj = s1;
                    //}

                    //// Find the coordinates of the intersection point in the segment's local system and the element's natural system
                    //double k = -si / (sj - si);
                    //double r = ri + k * (rj - ri);
                    //double xi = Mi.Xi + k * (Mj.Xi - Mi.Xi); 
                    //double eta = Mi.Eta + k * (Mj.Eta - Mi.Eta);
                    #endregion
                }
            }
            return intersectionPoints;
        }

        private NaturalPoint[] FindSingleNodeOnSegment(IXFiniteElement element, Dictionary<XNode, 
            (double r, double s)> localNodeCoordinates, double meshTol)
        {
            // Find the node that lies on the whole line
            NaturalPoint P0 = null;
            double r0 = double.NaN;
            for (int n = 0; n < element.Nodes.Count; ++n)
            {
                (double r, double s) = localNodeCoordinates[element.Nodes[n]];
                if (Math.Abs(s) < meshTol)
                {
                    P0 = element.InterpolationStandard.NodalNaturalCoordinates[n];
                    r0 = r;
                    break;
                }

            }

            // Check if it lies on the line segment as well
            if ((r0 >= 0) && (r0 <= Length)) return new NaturalPoint[] { P0 };
            else return new NaturalPoint[0];
        }

        /// <summary>
        /// Even if false is returned, the element and segment can still be disjoint. However this method serves to quickly 
        /// process most of the elements in the mesh.
        /// </summary>
        /// <param name="localNodeCoordinates"></param>
        /// <returns></returns>
        private bool IsElementFarAway(Dictionary<XNode, (double r, double s)> localNodeCoordinates)
        {
            double minR = double.MaxValue, maxR = double.MinValue;
            double minS = double.MaxValue, maxS = double.MinValue;
            foreach ((double r, double s) in localNodeCoordinates.Values)
            {
                if (r < minR) minR = r;
                if (r > maxR) maxR = r;
                if (s < minS) minS = s;
                if (s > maxS) maxS = s;
            }

            if (minS * maxS > 0.0) return true; // Not intersected by the segment or its extension
            else if ((minR > Length) || (maxR < 0)) return true; // Intersected the segment's extension
            else return false;
        }
        private NaturalPoint TransformLocalToNatural(double r, NaturalPoint P1, double r1, NaturalPoint P2, double r2)
        {
            double k = (r - r1) / (r2 - r1);
            double xi = P1.Xi + k * (P2.Xi - P1.Xi);
            double eta = P1.Eta + k * (P2.Eta - P1.Eta);
            return new NaturalPoint(xi, eta);
        }

        private class LocalRComparer : IComparer<double>
        {
            private readonly double meshTol;

            public LocalRComparer(double meshTol)
            {
                this.meshTol = meshTol;
            }

            public int Compare(double x, double y)
            {
                if (Math.Abs(x - y) <= meshTol) return 0;
                else if (x < y) return -1;
                else return 1;
            }
        }
    }
}