using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Integration;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Integration;

namespace MGroup.XFEM.Geometry
{
    public class NullElementIntersection3D : IElementSurfaceIntersection3D
    {
        public RelativePositionCurveElement RelativePosition => RelativePositionCurveElement.Disjoint;

        public IntersectionMesh ApproximateGlobalCartesian() => new IntersectionMesh();

        public IReadOnlyList<GaussPoint> GetIntegrationPoints(int numPoints)
        {
            return new GaussPoint[0];
        }

        public IList<double[]> GetPointsForTriangulation()
        {
            return new double[0][];
        }

    }
}
