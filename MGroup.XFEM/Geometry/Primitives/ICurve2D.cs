using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.XFEM.Geometry.Primitives
{
    public interface ICurve2D
    {
        double SignedDistanceOf(double[] point);

        IIntersectionCurve2D IntersectPolygon(IList<double[]> nodes);
    }
}
