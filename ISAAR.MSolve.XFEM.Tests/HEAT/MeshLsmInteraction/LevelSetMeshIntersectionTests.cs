using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;
using ISAAR.MSolve.XFEM.Thermal.Elements;
using ISAAR.MSolve.XFEM.Thermal.Entities;
using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod;
using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod.MeshInteraction;
using Xunit;

//TODO: Plot the intersections is a private constant is set to a non null path.
//TODO: Standarize ploting intersections (e.g. intersection points, intersection segment, tangent points, tangent segment).
namespace ISAAR.MSolve.XFEM.Tests.HEAT.MeshLsmInteraction
{
    public static class LevelSetMeshIntersectionTests
    {
        [Theory]
        [InlineData(CellType.Quad4, 0)]
        [InlineData(CellType.Quad4, 1)]
        [InlineData(CellType.Quad4, 2)]
        [InlineData(CellType.Quad4, 3)]
        [InlineData(CellType.Tri3, 0)]
        [InlineData(CellType.Tri3, 1)]
        [InlineData(CellType.Tri3, 2)]
        public static void TestDisjointElement(CellType cellType, int numNodeOrderRotations)
        {
            IXFiniteElement element = CreateElement(cellType, numNodeOrderRotations);
            var curve = new PolyLine2D(new CartesianPoint(-1.2, 1.0), new CartesianPoint(1.0, 2.0));
            var lsmCurve = new SimpleLsmCurve2D();
            lsmCurve.InitializeGeometry(element.Nodes, curve);

            CurveElementIntersection intersection = lsmCurve.IntersectElement(element);
            Assert.Equal(RelativePositionCurveElement.Disjoint, intersection.RelativePosition);
            Assert.Empty(intersection.IntersectionPoints);
            Assert.Empty(intersection.ContactNodes);
        }

        [Theory]
        [InlineData(CellType.Quad4, 0)]
        [InlineData(CellType.Quad4, 1)]
        [InlineData(CellType.Quad4, 2)]
        [InlineData(CellType.Quad4, 3)]
        [InlineData(CellType.Tri3, 0)]
        [InlineData(CellType.Tri3, 1)]
        [InlineData(CellType.Tri3, 2)]
        public static void TestIntersectedElementNoNodes(CellType cellType, int numNodeOrderRotations)
        {
            IXFiniteElement element = CreateElement(cellType, numNodeOrderRotations);
            var curve = new PolyLine2D(new CartesianPoint(-1.0, 0.0), new CartesianPoint(0.0, -1.0));
            var lsmCurve = new SimpleLsmCurve2D();
            lsmCurve.InitializeGeometry(element.Nodes, curve);

            CurveElementIntersection intersection = lsmCurve.IntersectElement(element);
            Assert.Equal(RelativePositionCurveElement.Intersection, intersection.RelativePosition);
            Assert.Empty(intersection.ContactNodes);
            Assert.Equal(2, intersection.IntersectionPoints.Length);

            var intersectionPoints = intersection.IntersectionPoints.Select(
                natural => element.StandardInterpolation.TransformNaturalToCartesian(element.Nodes, natural));
            CartesianPoint leftPoint = intersectionPoints.OrderBy(p => p.X).First();
            CartesianPoint rightPoint = intersectionPoints.OrderBy(p => p.X).Last();
            int precision = 10;
            Assert.Equal(-1.0, leftPoint.X, precision);
            Assert.Equal(0.0, leftPoint.Y, precision);
            Assert.Equal(0.0, rightPoint.X, precision);
            Assert.Equal(-1.0, rightPoint.Y, precision);
        }

        [Theory]
        [InlineData(CellType.Quad4, 0)]
        [InlineData(CellType.Quad4, 1)]
        [InlineData(CellType.Quad4, 2)]
        [InlineData(CellType.Quad4, 3)]
        [InlineData(CellType.Tri3, 0)]
        [InlineData(CellType.Tri3, 1)]
        [InlineData(CellType.Tri3, 2)]
        public static void TestIntersectedElementOneNode(CellType cellType, int numNodeOrderRotations)
        {
            IXFiniteElement element = CreateElement(cellType, numNodeOrderRotations);
            var curve = new PolyLine2D(new CartesianPoint(-1.0, 0.0), new CartesianPoint(1.0, -1.0));
            var lsmCurve = new SimpleLsmCurve2D();
            lsmCurve.InitializeGeometry(element.Nodes, curve);

            CurveElementIntersection intersection = lsmCurve.IntersectElement(element);
            Assert.Equal(RelativePositionCurveElement.Intersection, intersection.RelativePosition);
            Assert.Empty(intersection.ContactNodes);
            Assert.Equal(2, intersection.IntersectionPoints.Length);

            var intersectionPoints = intersection.IntersectionPoints.Select(
                natural => element.StandardInterpolation.TransformNaturalToCartesian(element.Nodes, natural));
            CartesianPoint leftPoint = intersectionPoints.OrderBy(p => p.X).First();
            CartesianPoint rightPoint = intersectionPoints.OrderBy(p => p.X).Last();
            int precision = 10;
            Assert.Equal(-1.0, leftPoint.X, precision);
            Assert.Equal(0.0, leftPoint.Y, precision);
            Assert.Equal(1.0, rightPoint.X, precision);
            Assert.Equal(-1.0, rightPoint.Y, precision);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public static void TestIntersectedElementOnlyNodes(int numNodeOrderRotations)
        {
            IXFiniteElement element = CreateElement(CellType.Quad4, numNodeOrderRotations);
            var curve = new PolyLine2D(new CartesianPoint(-1.0, -1.0), new CartesianPoint(1.0, 1.0));
            var lsmCurve = new SimpleLsmCurve2D();
            lsmCurve.InitializeGeometry(element.Nodes, curve);

            CurveElementIntersection intersection = lsmCurve.IntersectElement(element);
            Assert.Equal(RelativePositionCurveElement.Intersection, intersection.RelativePosition);
            Assert.Empty(intersection.ContactNodes);
            Assert.Equal(2, intersection.IntersectionPoints.Length);

            var intersectionPoints = intersection.IntersectionPoints.Select(
                natural => element.StandardInterpolation.TransformNaturalToCartesian(element.Nodes, natural));
            CartesianPoint topPoint = intersectionPoints.OrderBy(p => p.Y).Last();
            CartesianPoint bottomPoint = intersectionPoints.OrderBy(p => p.Y).First();
            int precision = 10;
            Assert.Equal(1.0, topPoint.X, precision);
            Assert.Equal(1.0, topPoint.Y, precision);
            Assert.Equal(-1.0, bottomPoint.X, precision);
            Assert.Equal(-1.0, bottomPoint.Y, precision);
        }

        [Theory]
        [InlineData(CellType.Quad4, 0)]
        [InlineData(CellType.Quad4, 1)]
        [InlineData(CellType.Quad4, 2)]
        [InlineData(CellType.Quad4, 3)]
        [InlineData(CellType.Tri3, 0)]
        [InlineData(CellType.Tri3, 1)]
        [InlineData(CellType.Tri3, 2)]
        public static void TestTangentAtEdgeElement(CellType cellType, int numNodeOrderRotations)
        {
            IXFiniteElement element = CreateElement(cellType, numNodeOrderRotations);
            var curve = new PolyLine2D(new CartesianPoint(-1.0, -2.0), new CartesianPoint(-1.0, 2.0));
            var lsmCurve = new SimpleLsmCurve2D();
            lsmCurve.InitializeGeometry(element.Nodes, curve);

            CurveElementIntersection intersection = lsmCurve.IntersectElement(element);
            Assert.Equal(RelativePositionCurveElement.TangentAlongElementEdge, intersection.RelativePosition);
            Assert.Equal(2, intersection.IntersectionPoints.Length);
            Assert.Equal(2, intersection.ContactNodes.Length);

            XNode topNode = intersection.ContactNodes.OrderBy(n => n.Y).Last();
            XNode bottomNode = intersection.ContactNodes.OrderBy(n => n.Y).First();
            Assert.Equal(-1.0, topNode.X);
            Assert.Equal(1.0, topNode.Y);
            Assert.Equal(-1.0, bottomNode.X);
            Assert.Equal(-1.0, bottomNode.Y);
        }

        [Theory]
        [InlineData(CellType.Quad4, 0)]
        [InlineData(CellType.Quad4, 1)]
        [InlineData(CellType.Quad4, 2)]
        [InlineData(CellType.Quad4, 3)]
        [InlineData(CellType.Tri3, 0)]
        [InlineData(CellType.Tri3, 1)]
        [InlineData(CellType.Tri3, 2)]
        public static void TestTangentAtNodeElement(CellType cellType, int numNodeOrderRotations)
        {
            IXFiniteElement element = CreateElement(cellType, numNodeOrderRotations);
            var curve = new PolyLine2D(new CartesianPoint(-1.0, 1.0), new CartesianPoint(1.0, 2.0));
            var lsmCurve = new SimpleLsmCurve2D();
            lsmCurve.InitializeGeometry(element.Nodes, curve);

            CurveElementIntersection intersection = lsmCurve.IntersectElement(element);
            Assert.Equal(RelativePositionCurveElement.TangentAtSingleNode, intersection.RelativePosition);
            Assert.Single(intersection.IntersectionPoints);
            Assert.Single(intersection.ContactNodes);
            Assert.Equal(-1.0, intersection.ContactNodes[0].X);
            Assert.Equal(1.0, intersection.ContactNodes[0].Y);
        }

        private static XThermalElement2D CreateElement(CellType cellType, int numNodeOrderRotations)
        {
            var factory = new XThermalElement2DFactory(null, 0.0, null, 0);
            if (cellType == CellType.Quad4)
            {
                var nodes = new XNode[]
                {
                    new XNode(0, -1.0, -1.0),
                    new XNode(0, 1.0, -1.0),
                    new XNode(0, 1.0, 1.0),
                    new XNode(0, -1.0, 1.0)
                };
                nodes = RotateArrayEntries(nodes, numNodeOrderRotations);
                return factory.CreateElement(0, CellType.Quad4, nodes);
            }
            else if (cellType == CellType.Tri3)
            {
                var nodes = new XNode[]
                {
                    new XNode(0, -1.0, -1.0),
                    new XNode(0, 1.0, -1.0),
                    new XNode(0, -1.0, 1.0)
                };
                nodes = RotateArrayEntries(nodes, numNodeOrderRotations);
                return factory.CreateElement(0, CellType.Tri3, nodes);
            }
            else throw new NotImplementedException();
        }

        private static T[] RotateArrayEntries<T>(T[] originalOrder, int numRightwiseRotations)
        {
            int numEntries = originalOrder.Length;
            T[] currentOrder = originalOrder;
            for (int rot = 0; rot < numRightwiseRotations; ++rot)
            {
                var nextOrder = new T[originalOrder.Length];
                for (int i = 0; i < numEntries; ++i)
                {
                    nextOrder[(i + 1) % numEntries] = currentOrder[i];
                }
                currentOrder = nextOrder;
            }
            return currentOrder;
        }
    }
}
