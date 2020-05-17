using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;

namespace MGroup.XFEM.Geometry.Primitives
{
    /// <summary>
    /// A curve resulting from the intersection of a parent curve with a disc (the part of the plane enclosed in a closed curved).
    /// Degenerate cases are also possible: null or single point
    /// </summary>
    public interface IIntersectionSurface3D
    {
        RelativePositionCurveElement RelativePosition { get; }

        GaussPoint[] GetIntersectionPoints(int numPoints);

        IList<double[]> GetPointsForTriangulation(); 
    }
}
