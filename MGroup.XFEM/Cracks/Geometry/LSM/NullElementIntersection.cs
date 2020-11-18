using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Integration;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Geometry;

//TODO: Merge this with the one from MGroup.XFEM.Geometry
namespace MGroup.XFEM.Cracks.Geometry.LSM
{
    public class NullElementIntersection : IElementCrackInteraction
    {
        public NullElementIntersection(int parentGeometryID, IXFiniteElement element)
        {
            this.ParentGeometryID = parentGeometryID;
            this.Element = element;
        }

        public int ParentGeometryID { get; }

        public RelativePositionCurveElement RelativePosition => RelativePositionCurveElement.Disjoint;

        public IXFiniteElement Element { get; }

        public bool TipInteractsWithElement => false;

        public IIntersectionMesh ApproximateGlobalCartesian() => new IntersectionMesh();

        public IReadOnlyList<GaussPoint> GetBoundaryIntegrationPoints(int numPoints)
        {
            return new GaussPoint[0];
        }

        public IList<double[]> GetVerticesForTriangulation()
        {
            return new double[0][];
        }
    }
}
