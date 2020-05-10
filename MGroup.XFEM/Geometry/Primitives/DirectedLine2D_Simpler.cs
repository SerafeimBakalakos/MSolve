using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using DotNumerics.Optimization.TN;
using ISAAR.MSolve.LinearAlgebra;

namespace MGroup.XFEM.Geometry.Primitives
{
    public class DirectedLine2D_Simpler : ILine2D
    {
        /// <summary>
        /// a is the counter-clockwise angle from the global x axis to the local x axis
        /// Transformation matrix from global to local system: Q = [cosa sina; -sina cosa ]
        /// Transformation matrix from local to global system: Q^T = [cosa -sina; sina cosa ]
        /// </summary>
        private readonly double cosa, sina;

        ///// <summary>
        ///// The coordinates of the global system's origin in the local system
        ///// </summary>
        private readonly double[] originLocal;

        /// <summary>
        /// The unit vector that is perpendicular to the segment and faces towards the positive local y axis. 
        /// It is constant for a linear segment, so caching it avoids recalculations.
        /// </summary>
        private readonly double[] normalVector;

        /// <summary>
        /// Directed from <paramref name="point0"/> to <paramref name="point1"/>.
        /// </summary>
        /// <param name="point0"></param>
        /// <param name="point1"></param>
        /// <param name="sys"></param>
        public DirectedLine2D_Simpler(double[] point0, double[] point1)
        {
            double dx = point1[0] - point0[0];
            double dy = point1[1] - point0[1];
            double length = Math.Sqrt(dx * dx + dy * dy);
            cosa = dx / length;
            sina = dy / length;

            originLocal = new double[2];
            originLocal[0] = -cosa * point0[0] - sina * point0[1];
            originLocal[1] = sina * point0[0] - cosa * point0[1];

            normalVector = new double[] { -sina, cosa };
        }

        /// <summary>
        /// See https://en.wikipedia.org/wiki/Distance_from_a_point_to_a_line#Vector_formulation
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public double SignedDistanceOf(double[] point)
        {
            return -sina * point[0] + cosa * point[1] + originLocal[1];
        }

        /// <summary>
        /// Returns a characterization of the relative position and a list of values for the parameter t. Each value corresponds
        /// to an intersection point: r(t) = A + t * s
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        public (RelativePositionCurveCurve, double[]) IntersectPolygon(IList<double[]> nodes)
        {
            //TODO: Use the results to create a new geometric object that can provide vertices for triangulation, gauss points etc. These can be empty
            //TODO: needs a fast way to eleminate most elements

            // Find the projection and perpendicular vectors of these points onto the line. See SignedDistance() for more details
            var nodesLocal = new double[nodes.Count][];
            for (int i = 0; i < nodes.Count; ++i)
            {
                nodesLocal[i] = ProjectToLocal(nodes[i]);
            }

            // Intersect each segment
            var intersections = new SortedSet<double>();
            bool conformingSegment = false;
            for (int i = 0; i < nodes.Count; ++i)
            {
                int nextIndex = (i + 1) % nodes.Count;
                (RelativePositionCurveCurve pos, double[] segmentIntersections)
                    = IntersectSegment(nodesLocal[i], nodesLocal[nextIndex]);
                if (pos == RelativePositionCurveCurve.Conforming) conformingSegment = true;
                foreach (double t in segmentIntersections) intersections.Add(t);
            }

            // Investigate the intersection type
            if (intersections.Count == 0)
            {
                return (RelativePositionCurveCurve.Disjoint, new double[0]);
            }
            else if (intersections.Count == 1)
            {
                return (RelativePositionCurveCurve.Tangent, new double[] { intersections.First() });
            }
            else if (intersections.Count == 2)
            {
                if (conformingSegment) return (RelativePositionCurveCurve.Conforming, intersections.ToArray());
                else return (RelativePositionCurveCurve.Intersection, intersections.ToArray());
            }
            else throw new Exception();
        }

        /// <summary>
        /// Returns a characterization of the relative position and a list of local X coordinates. Each value corresponds
        /// to an intersection point.
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        private (RelativePositionCurveCurve, double[]) IntersectSegment(double[] point1Local, double[] point2Local)
        { //TODO: Use the results to create a new geometric object that can provide vertices for triangulation, gauss points etc. These can be empty

            double product = point1Local[1] * point2Local[1];
            if (product > 0) // Parallel or otherwise disjoint
            {
                return (RelativePositionCurveCurve.Disjoint, new double[0]);
            }
            else if (product < 0) // P1, P2 lie on opposite semiplanes
            {
                // Use linear interpolation
                double lambda = -point1Local[1] / (point2Local[1] - point1Local[1]);
                double intersectionLocalX = point1Local[0] + lambda * (point2Local[0] - point1Local[0]);
                return (RelativePositionCurveCurve.Intersection, new double[] { intersectionLocalX });
            }
            else
            {
                if (point1Local[1] == 0 && point2Local[1] == 0) // Both P1 and P2 lie on the line
                {
                    return (RelativePositionCurveCurve.Conforming, Sort(point1Local[0], point2Local[0]));
                }
                else if (point1Local[1] == 0 /*&& point2Local[1] != 0*/) // Only P1 lies on the line
                {
                    return (RelativePositionCurveCurve.Intersection, new double[] { point1Local[0] });
                }
                else /*(point1Local[1] != 0 && point2Local[1] == 0)*/ // Only P2 lies on the line
                {
                    return (RelativePositionCurveCurve.Intersection, new double[] { point2Local[0] });
                }
            }
        }


        public double[] LocalToGlobal(double localX)
        {
            // xGlobal = Q^T * (xLocal - originLocal)
            double dx = localX - originLocal[0];
            double dy = - originLocal[1];
            return new double[2]
            {
                cosa * dx - sina * dy,
                sina * dx + cosa * dy
            };
        }


        public double[] NormalVectorThrough(double[] point)
        {
            throw new NotImplementedException();
        }

        private double[] ProjectToLocal(double[] pointGlobal)
        {
            var pointLocal = new double[2];
            pointLocal[0] = cosa * pointGlobal[0] + sina * pointGlobal[1] + originLocal[0];
            pointLocal[1] = -sina * pointGlobal[0] + cosa * pointGlobal[1] + originLocal[1];
            return pointLocal;
        }

        private double[] Sort(double val1, double val2)
        {
            if (val1 < val2) return new double[] { val1, val2 };
            else if (val1 > val2) return new double[] { val2, val1 };
            else throw new NotImplementedException();
        }
    }
}
