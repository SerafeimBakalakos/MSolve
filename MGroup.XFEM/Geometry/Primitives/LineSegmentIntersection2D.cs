using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MGroup.XFEM.Integration;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Discretization.Integration;

//TODO: remove duplication between this and Line2D & LineSegment2D. Why can't this inherit from LineSegment2D? 
//      Or just use LineSegment2D wrapped in a class about Intersection
namespace MGroup.XFEM.Geometry.Primitives
{
    public class LineSegmentIntersection2D : IElementCurveIntersection2D
    {
        /// <summary>
        /// a is the counter-clockwise angle from the global x axis to the local x axis
        /// Transformation matrix from global to local system: Q = [cosa sina; -sina cosa ]
        /// Transformation matrix from local to global system: Q^T = [cosa -sina; sina cosa ]
        /// </summary>
        protected readonly double cosa, sina;

        ///// <summary>
        ///// The coordinates of the global system's origin in the local system
        ///// </summary>
        protected readonly double[] originLocal;

        public LineSegmentIntersection2D(RelativePositionCurveElement pos, double cosa, double sina, double[] originLocal, 
            double startLocalX, double endLocalX)
        {
            Debug.Assert(pos == RelativePositionCurveElement.Intersecting || pos == RelativePositionCurveElement.Conforming);
            this.RelativePosition = pos;
            this.cosa = cosa;
            this.sina = sina;
            this.originLocal = originLocal;
            this.StartLocalX = startLocalX;
            this.EndLocalX = endLocalX;

            this.StartGlobalCartesian = ProjectLocalToGlobal(startLocalX);
            this.EndGlobalCartesian = ProjectLocalToGlobal(endLocalX);
        }

        public LineSegmentIntersection2D(double[] start, double[] end, RelativePositionCurveElement pos)
        {
            Debug.Assert(pos == RelativePositionCurveElement.Intersecting || pos == RelativePositionCurveElement.Conforming);
            this.StartGlobalCartesian = start;
            this.EndGlobalCartesian = end;
            this.RelativePosition = pos;

            double dx = end[0] - start[0];
            double dy = end[1] - start[1];

            double length = Math.Sqrt(dx * dx + dy * dy);
            this.cosa = dx / length;
            this.sina = dy / length;

            this.originLocal = new double[2];
            this.originLocal[0] = -cosa * start[0] - sina * start[1];
            this.originLocal[1] = sina * start[0] - cosa * start[1];
        }

        public RelativePositionCurveElement RelativePosition { get; }

        public double[] StartGlobalCartesian { get; }

        public double StartLocalX { get; }

        public double[] EndGlobalCartesian { get; }

        public double EndLocalX { get; }

        public List<double[]> ApproximateGlobalCartesian()
        {
            var points = new List<double[]>(2);
            points.Add(StartGlobalCartesian);
            points.Add(EndGlobalCartesian);
            return points;
        }

        public IReadOnlyList<GaussPoint> GetIntegrationPoints(int numPoints)
        {
            // If conforming: halve the weights. Perhaps this can be done in the XElement
            throw new NotImplementedException();
        }

        public NaturalPoint[] GetPointsForTriangulation()
        {
            throw new NotImplementedException();
        }

        private double[] ProjectLocalToGlobal(double localX)
        {
            // xGlobal = Q^T * (xLocal - originLocal)
            double dx = localX - originLocal[0];
            double dy = -originLocal[1];
            return new double[2]
            {
                cosa * dx - sina * dy,
                sina * dx + cosa * dy
            };
        }
    }
}
