using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Geometry.Coordinates;

namespace MGroup.XFEM.Geometry
{
    public class NullElementIntersection3D : IElementSurfaceIntersection3D
    {
        public RelativePositionCurveElement RelativePosition => RelativePositionCurveElement.Disjoint;

        public IntersectionMesh<CartesianPoint> ApproximateGlobalCartesian() => new IntersectionMesh<CartesianPoint>();

        public GaussPoint[] GetIntegrationPoints(int numPoints)
        {
            return new GaussPoint[0];
        }

        public IList<NaturalPoint> GetPointsForTriangulation()
        {
            return new NaturalPoint[0];
        }

    }
}
