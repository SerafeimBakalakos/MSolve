//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using ISAAR.MSolve.Geometry.Coordinates;
//using ISAAR.MSolve.Geometry.Shapes;
//using ISAAR.MSolve.Geometry.Triangulation;
//using ISAAR.MSolve.XFEM.Thermal.Elements;
//using ISAAR.MSolve.XFEM.Thermal.Entities;
//using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod.MeshInteraction;

//namespace ISAAR.MSolve.XFEM.Thermal.Curves.LevelSetMethod
//{
//    public class MultiLsmClosedCurve2D : ILsmCurve2D
//    {
//        private readonly double zeroTolerance;

//        public MultiLsmClosedCurve2D(double interfaceThickness = 1.0, double zeroTolerance = 1E-13)
//        {
//            this.SingleCurves = new List<SimpleLsmClosedCurve2D>();
//            this.Thickness = interfaceThickness;
//            this.zeroTolerance = zeroTolerance;
//        }

//        public List<SimpleLsmClosedCurve2D> SingleCurves { get; }

//        public double Thickness { get; }

//        public void InitializeGeometry(IEnumerable<XNode> nodes, IReadOnlyList<ICurve2D> discontinuities)
//        {
//            foreach (ICurve2D discontinuity in discontinuities)
//            {
//                var singleCurve = new SimpleLsmClosedCurve2D(Thickness, zeroTolerance);
//                singleCurve.InitializeGeometry(nodes, discontinuity);
//                SingleCurves.Add(singleCurve);
//            }
//        }

//        public CurveElementIntersection IntersectElement(IXFiniteElement element)
//        {
//            foreach (SimpleLsmClosedCurve2D curve in SingleCurves)
//            {
//                var intersection = curve.IntersectElement(element);
//                if (intersection.RelativePosition != RelativePositionCurveElement.Disjoint) return intersection;
//            }
//            return new CurveElementIntersection(RelativePositionCurveElement.Disjoint, new NaturalPoint[0], new XNode[0]);
//        }

//        public double SignedDistanceOf(XNode node)
//        {
//            double min = double.MaxValue;
//            foreach (SimpleLsmClosedCurve2D curve in SingleCurves)
//            {
//                double signedDistance = curve.SignedDistanceOf(node);
//                if (signedDistance < min) min = signedDistance;
//            }
//            return min;
//        }

//        public double SignedDistanceOf(IXFiniteElement element, double[] shapeFunctionsAtNaturalPoint)
//        {
//            double min = double.MaxValue;
//            foreach (SimpleLsmClosedCurve2D curve in SingleCurves)
//            {
//                double signedDistance = curve.SignedDistanceOf(element, shapeFunctionsAtNaturalPoint);
//                if (signedDistance < min) min = signedDistance;
//            }
//            return min;
//        }

//        public bool TryConformingTriangulation(IXFiniteElement element, CurveElementIntersection intersectionIgnored, 
//            out IReadOnlyList<ElementSubtriangle> subtriangles)
//        {
//            // Gather nodes and intersection points from all curves. 
//            //TODO: This assumes that the curves do not intersect each other
//            var comparer = new Point2DComparerXMajor<NaturalPoint>(1E-7); //TODO: This should be avoided. It should also be avoided in the single curve version.
//            var triangleVertices = new SortedSet<NaturalPoint>(comparer); //TODO: Better use a HashSet, which needs a hash function for points.
//            foreach (SimpleLsmClosedCurve2D curve in SingleCurves)
//            {
//               var intersection = curve.IntersectElement(element);
//                if (intersection.RelativePosition != RelativePositionCurveElement.Disjoint)
//                {
//                    triangleVertices.UnionWith(curve.FindConformingTriangleVertices(element, intersection));
//                }
//            }

//            // Create triangles
//            if (triangleVertices.Count > 0)
//            {
//                var triangulator = new Triangulator2D<NaturalPoint>((x1, x2) => new NaturalPoint(x1, x2));
//                List<Triangle2D<NaturalPoint>> triangles = triangulator.CreateMesh(triangleVertices);
//                subtriangles = triangles.Select(t => new ElementSubtriangle(t.Vertices)).ToList();
//                return true;
//            }
//            else
//            {
//                subtriangles = null;
//                return false;
//            }
//        }
//    }
//}
