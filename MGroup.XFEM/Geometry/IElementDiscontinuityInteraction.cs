using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Elements;

namespace MGroup.XFEM.Geometry
{
    public interface IElementDiscontinuityInteraction
    {
        IXFiniteElement Element { get; }

        int ParentGeometryID { get; }

        RelativePositionCurveElement RelativePosition { get; }

        IIntersectionMesh ApproximateGlobalCartesian();

        /// <summary>
        /// The weights of the returned <see cref="GaussPoint"/>s include the determinant of the Jacobian from the
        /// natural system of the element to the global cartesian system.
        /// </summary>
        /// <param name="order"></param>
        IReadOnlyList<GaussPoint> GetBoundaryIntegrationPoints(int order);

        IList<double[]> GetVerticesForTriangulation();
    }
}
