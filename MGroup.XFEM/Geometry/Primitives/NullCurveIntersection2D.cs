using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;

namespace MGroup.XFEM.Geometry.Primitives
{
    public class NullCurveIntersection2D : IIntersectionCurve2D
    {
        public RelativePositionCurveElement RelativePosition => RelativePositionCurveElement.Disjoint;

        public double[] Start => null;

        public double[] End => null;

        public GaussPoint[] GetIntersectionPoints(int numPoints)
        {
            return new GaussPoint[0];
        }

        public IList<double[]> GetPointsForTriangulation()
        {
            return new double[0][];
        }
    }
}
