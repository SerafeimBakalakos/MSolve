using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Geometry.Coordinates;

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

        /// <summary>
        /// The weights of the returned <see cref="GaussPoint"/>s include the determinant of the Jacobian from the
        /// natural system of the element to the global cartesian system.
        /// </summary>
        /// <param name="order"></param>
        GaussPoint[] GetIntegrationPoints(int order);

        IList<NaturalPoint> GetPointsForTriangulation(); 
    }
}
