using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Geometry.LSM;

namespace MGroup.XFEM.Geometry
{
    public class NullElementDiscontinuityInteraction : IElementDiscontinuityInteraction, IElementOpenGeometryInteraction
    {
        public NullElementDiscontinuityInteraction(int parentGeometryID, IXFiniteElement element)
        {
            this.ParentGeometryID = parentGeometryID;
            this.Element = element;
        }

        public int ParentGeometryID { get; }

        public RelativePositionCurveElement RelativePosition => RelativePositionCurveElement.Disjoint;

        public IXFiniteElement Element { get; }

        public bool TipInteractsWithElement => false;

        public IntersectionMesh ApproximateGlobalCartesian() => new IntersectionMesh();

        public IReadOnlyList<GaussPoint> GetBoundaryIntegrationPoints(int numPoints)
        {
            return new GaussPoint[0];
        }

        public IList<double[]> GetVerticesForTriangulation()
        {
            return new double[0][];
        }

        IIntersectionMesh IElementDiscontinuityInteraction.ApproximateGlobalCartesian() => new IntersectionMesh();
    }
}
