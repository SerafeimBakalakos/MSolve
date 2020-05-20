using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.XFEM.Geometry.Primitives
{
    public class Circle2D : ICurve2D
    {
        public Circle2D(double centerX, double centerY, double radius)
        {
            this.Center = new double[] { centerX, centerY };
            this.Radius = radius;
        }

        public double[] Center { get; }
        public double Radius { get; }

        public IElementCurveIntersection2D IntersectPolygon(IList<double[]> nodes)
        {
            throw new NotImplementedException();
        }

        public double SignedDistanceOf(double[] point)
        {
            return point.Distance2D(Center) - Radius;
        }
    }
}
