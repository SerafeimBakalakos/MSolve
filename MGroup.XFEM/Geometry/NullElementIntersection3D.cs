using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Integration;
using ISAAR.MSolve.Geometry.Coordinates;

namespace MGroup.XFEM.Geometry
{
    public class NullElementIntersection3D : IElementSurfaceIntersection3D
    {
        public RelativePositionCurveElement RelativePosition => RelativePositionCurveElement.Disjoint;

        public IntersectionMesh<CartesianPoint> ApproximateGlobalCartesian() => new IntersectionMesh<CartesianPoint>();

        public IReadOnlyList<GaussPoint> GetIntegrationPoints(int numPoints)
        {
            return new GaussPoint[0];
        }

        public NaturalPoint[] GetPointsForTriangulation()
        {
            return new NaturalPoint[0];
        }

    }
}
