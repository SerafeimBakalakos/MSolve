using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;

namespace MGroup.XFEM.Geometry
{
    public class NullElementCurveIntersection2D : IElementCurveIntersection2D
    {
        public RelativePositionCurveElement RelativePosition => RelativePositionCurveElement.Disjoint;

        public List<double[]> ApproximateGlobalCartesian() => new List<double[]>(0);

        public GaussPoint[] GetIntegrationPoints(int numPoints)
        {
            return new GaussPoint[0];
        }

        public IList<double[]> GetPointsForTriangulation()
        {
            return new double[0][];
        }
    }
}
