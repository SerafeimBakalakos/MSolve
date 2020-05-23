using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;

namespace MGroup.XFEM.Geometry
{
    /// <summary>
    /// A curve resulting from the intersection of a parent curve with a 2D element.
    /// Degenerate cases are also possible: null or single point.
    /// </summary>
    public interface IElementCurveIntersection2D
    {
        RelativePositionCurveElement RelativePosition { get; }

        List<double[]> ApproximateGlobalCartesian();

        GaussPoint[] GetIntegrationPoints(int numPoints);

        IList<double[]> GetPointsForTriangulation(); 
    }
}
