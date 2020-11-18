using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Integration;

namespace MGroup.XFEM.Cracks.Geometry.LSM
{
    public class OpenLsmElementIntersection2D : IElementCrackInteraction
    {
        private readonly IList<double[]> commonPointsNatural;

        public OpenLsmElementIntersection2D(int parentGeometryID, IXFiniteElement element, 
            RelativePositionCurveElement relativePosition, bool tipInteractsWithElement, IList<double[]> commonPointsNatural)
        {
            this.ParentGeometryID = parentGeometryID;
            this.Element = element;
            if (relativePosition == RelativePositionCurveElement.Disjoint)
            {
                throw new ArgumentException("There is no intersection between the curve and element");
            }
            this.RelativePosition = relativePosition;
            this.TipInteractsWithElement = tipInteractsWithElement;
            this.commonPointsNatural = commonPointsNatural;
        }

        public IXFiniteElement Element { get; }

        public int ParentGeometryID { get; }

        public RelativePositionCurveElement RelativePosition { get; }

        public bool TipInteractsWithElement { get; }

        public IIntersectionMesh ApproximateGlobalCartesian()
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<GaussPoint> GetBoundaryIntegrationPoints(int order)
        {
            throw new NotImplementedException();
        }

        public IList<double[]> GetVerticesForTriangulation()
        {
            if (RelativePosition == RelativePositionCurveElement.Intersecting) return commonPointsNatural;
            else return new double[0][];
        }
    }
}
