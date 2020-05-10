using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.XFEM.Geometry.Primitives
{
    public interface ILine2D
    {
        double SignedDistanceOf(double[] point);

        //TODO: return points in global system directly or even better a new intersection object (e.g. LineSegment, PointIntersection, etc.)
        (RelativePositionCurveCurve, double[]) IntersectPolygon(IList<double[]> nodes);

        //TODO: remove this
        double[] LocalToGlobal(double localX);
    }
}
