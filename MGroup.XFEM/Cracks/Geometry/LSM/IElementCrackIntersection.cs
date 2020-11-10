using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Integration;

//TODO: Merge with the general IElementGeometryIntersection
namespace MGroup.XFEM.Cracks.Geometry.LSM
{
    public interface IElementCrackIntersection
    {
        int ElementID { get; }

        int ParentGeometryID { get; }

        RelativePositionCurveElement RelativePosition { get; }

        bool TipInteractsWithElement { get; }

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
