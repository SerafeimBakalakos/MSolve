using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;

namespace MGroup.XFEM.Geometry.Primitives
{
    public class LineSegmentIntersection2D : IIntersectionCurve2D
    {
        public LineSegmentIntersection2D(double[] start, double[] end, RelativePositionCurveDisc pos)
        {
            this.Start = start;
            this.End = end;
            Debug.Assert(pos == RelativePositionCurveDisc.Intersecting || pos == RelativePositionCurveDisc.Conforming);
            this.RelativePosition = pos;
        }

        public RelativePositionCurveDisc RelativePosition { get; }

        public double[] Start { get; }

        public double[] End { get; }

        public GaussPoint[] GetIntersectionPoints(int numPoints)
        {
            // If conforming: halve the weights. Perhaps this can be done in the XElement
            throw new NotImplementedException();
        }
    }
}
