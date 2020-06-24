using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Integration;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Integration;

namespace MGroup.XFEM.Geometry
{
    public interface IElementGeometryIntersection
    {
        RelativePositionCurveElement RelativePosition { get; }

        /// <summary>
        /// The weights of the returned <see cref="GaussPoint"/>s include the determinant of the Jacobian from the
        /// natural system of the element to the global cartesian system.
        /// </summary>
        /// <param name="order"></param>
        IReadOnlyList<GaussPoint> GetIntegrationPoints(int order);

        IList<double[]> GetPointsForTriangulation();
    }
}
