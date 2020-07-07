using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Integration;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Elements;

namespace MGroup.XFEM.Geometry
{
    public class NullElementIntersection : IElementGeometryIntersection
    {
        public NullElementIntersection(IXFiniteElement element)
        {
            this.Element = element;
        }

        public RelativePositionCurveElement RelativePosition => RelativePositionCurveElement.Disjoint;

        public IXFiniteElement Element { get; }

        public IntersectionMesh ApproximateGlobalCartesian() => new IntersectionMesh();

        public IReadOnlyList<GaussPoint> GetIntegrationPoints(int numPoints)
        {
            return new GaussPoint[0];
        }

        public IList<double[]> GetPointsForTriangulation()
        {
            return new double[0][];
        }

        IIntersectionMesh IElementGeometryIntersection.ApproximateGlobalCartesian() => new IntersectionMesh();
    }
}
