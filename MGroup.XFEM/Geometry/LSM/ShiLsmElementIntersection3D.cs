//using System;
//using System.Collections.Generic;
//using System.Text;
//using ISAAR.MSolve.Discretization.Integration.Quadratures;
//using MGroup.XFEM.Elements;
//using MGroup.XFEM.Geometry;
//using MGroup.XFEM.Geometry.LSM.Utilities;
//using MGroup.XFEM.Integration;

//namespace MGroup.XFEM.Geometry.LSM
//{
//    public class ShiLsmElementIntersection3D : IElementOpenGeometryInteraction
//    {
//        public ShiLsmElementIntersection3D(int parentGeometryID, IXFiniteElement element, 
//            RelativePositionCurveElement relativePosition, bool tipInteractsWithElement, 
//            IList<IntersectionPoint> intersectionPoints)
//        {
//            this.ParentGeometryID = parentGeometryID;
//            this.Element = element;
//            if (relativePosition == RelativePositionCurveElement.Disjoint)
//            {
//                throw new ArgumentException("There is no intersection between the curve and element");
//            }
//            this.RelativePosition = relativePosition;
//            this.TipInteractsWithElement = tipInteractsWithElement;
//            this.IntersectionPoints = intersectionPoints;
//        }

//        public IXFiniteElement Element { get; }

//        public IList<IntersectionPoint> IntersectionPoints { get; }

//        public int ParentGeometryID { get; }

//        public RelativePositionCurveElement RelativePosition { get; }

//        public bool TipInteractsWithElement { get; }

//        public IIntersectionMesh ApproximateGlobalCartesian()
//        {
//            throw new NotImplementedException();
//        }

//        public IReadOnlyList<GaussPoint> GetBoundaryIntegrationPoints(int order)
//        {
//            throw new NotImplementedException();
//        }

//        public IReadOnlyList<double[]> GetNormalsAtBoundaryIntegrationPoints(int order)
//        {
//            throw new NotImplementedException();
//        }

//        public IList<double[]> GetVerticesForTriangulation()
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
