using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Thermal.Curves.Explicit.Line;
using ISAAR.MSolve.XFEM.Thermal.Curves.MeshInteraction;
using ISAAR.MSolve.XFEM.Thermal.Elements;
using ISAAR.MSolve.XFEM.Thermal.Entities;
using Xunit;

namespace ISAAR.MSolve.XFEM.Tests.HEAT.CurveMeshInteraction
{
    public static class LinearSegmentMeshInteractionTests
    {
        [Fact]
        public static void TestDisjointElement()
        {
            //
            // ------------        /
            // |          |       /
            // |          |      /
            // ------------     /
            //
            IXFiniteElement element = CreateElement();
            var curve = new LineSegment2D(new CartesianPoint(7, 2), new CartesianPoint(6, 0), 1.0);

            CurveElementIntersection intersection = curve.IntersectElement(element);
            Assert.Equal(RelativePositionCurveElement.Disjoint, intersection.RelativePosition);
            Assert.Empty(intersection.IntersectionPoints);
            Assert.Empty(intersection.ContactNodes);
        }

        [Fact]
        public static void TestDisjointElementCutByExtension()
        {
            //              /    
            //             /
            //            /
            //           /
            // ------------    
            // |          |    
            // |          |    
            // ------------    
            //

            IXFiniteElement element = CreateElement();
            var curve = new LineSegment2D(new CartesianPoint(4, 4), new CartesianPoint(3.5, 1.2), 1.0);

            CurveElementIntersection intersection = curve.IntersectElement(element);
            Assert.Equal(RelativePositionCurveElement.Disjoint, intersection.RelativePosition);
            Assert.Empty(intersection.IntersectionPoints);
            Assert.Empty(intersection.ContactNodes);
        }

        [Fact]
        public static void TestIntersectedElement1Node()
        {
            //               /
            //             /
            // ------------    
            // |        / |    
            // |      /   |    
            // ------------    
            //     /
            //   /
            //

            IXFiniteElement element = CreateElement();
            var curve = new LineSegment2D(new CartesianPoint(6, 2), new CartesianPoint(0, -1), 1.0);

            CurveElementIntersection intersection = curve.IntersectElement(element);
            Assert.Equal(RelativePositionCurveElement.Intersection, intersection.RelativePosition);
            Assert.Empty(intersection.ContactNodes);
            Assert.Equal(2, intersection.IntersectionPoints.Length);

            var intersectionPoints = intersection.IntersectionPoints.Select(
                natural => element.StandardInterpolation.TransformNaturalToCartesian(element.Nodes, natural));
            CartesianPoint leftPoint = intersectionPoints.OrderBy(p => p.X).First();
            CartesianPoint rightPoint = intersectionPoints.OrderBy(p => p.X).Last();
            int precision = 10;
            Assert.Equal(2.0, leftPoint.X, precision);
            Assert.Equal(0.0, leftPoint.Y, precision);
            Assert.Equal(4.0, rightPoint.X, precision);
            Assert.Equal(1.0, rightPoint.Y, precision);
        }

        [Fact]
        public static void TestIntersectedElement2Nodes()
        {
            //                     /
            //                 /   
            //     ---------    
            //     |     / |    
            //     |/      |    
            //     ---------    
            // /   
            //

            IXFiniteElement element = CreateElement();
            var curve = new LineSegment2D(new CartesianPoint(8, 2), new CartesianPoint(-4, -1), 1.0);

            CurveElementIntersection intersection = curve.IntersectElement(element);
            Assert.Equal(RelativePositionCurveElement.Intersection, intersection.RelativePosition);
            Assert.Empty(intersection.ContactNodes);
            Assert.Equal(2, intersection.IntersectionPoints.Length);

            var intersectionPoints = intersection.IntersectionPoints.Select(
                natural => element.StandardInterpolation.TransformNaturalToCartesian(element.Nodes, natural));
            CartesianPoint leftPoint = intersectionPoints.OrderBy(p => p.X).First();
            CartesianPoint rightPoint = intersectionPoints.OrderBy(p => p.X).Last();
            int precision = 10;
            Assert.Equal(0.0, leftPoint.X, precision);
            Assert.Equal(0.0, leftPoint.Y, precision);
            Assert.Equal(4.0, rightPoint.X, precision);
            Assert.Equal(1.0, rightPoint.Y, precision);
        }

        [Fact]
        public static void TestIntersectedElement1PointNoNodes()
        {
            //            /
            //           /
            // ------------    
            // |       /  |    
            // |          |    
            // ------------    
            //

            IXFiniteElement element = CreateElement();
            var curve = new LineSegment2D(new CartesianPoint(4, 2), new CartesianPoint(2.5, 0.5), 1.0);

            CurveElementIntersection intersection = curve.IntersectElement(element);
            Assert.Equal(RelativePositionCurveElement.Intersection, intersection.RelativePosition);
            Assert.Empty(intersection.ContactNodes);
            Assert.Equal(2, intersection.IntersectionPoints.Length);

            var intersectionPoints = intersection.IntersectionPoints.Select(
                natural => element.StandardInterpolation.TransformNaturalToCartesian(element.Nodes, natural));
            CartesianPoint leftPoint = intersectionPoints.OrderBy(p => p.X).First();
            CartesianPoint rightPoint = intersectionPoints.OrderBy(p => p.X).Last();
            int precision = 10;
            Assert.Equal(2.5, leftPoint.X, precision);
            Assert.Equal(0.5, leftPoint.Y, precision);
            Assert.Equal(3.0, rightPoint.X, precision);
            Assert.Equal(1.0, rightPoint.Y, precision);
        }

        [Fact]
        public static void TestIntersectedElement2PointsNoNodes()
        {
            //            /
            //           /
            // ------------    
            // |       /  |    
            // |      /   |    
            // ------------    
            //      /
            //     /
            //

            IXFiniteElement element = CreateElement();
            var curve = new LineSegment2D(new CartesianPoint(4, 2), new CartesianPoint(1, -1), 1.0);

            CurveElementIntersection intersection = curve.IntersectElement(element);
            Assert.Equal(RelativePositionCurveElement.Intersection, intersection.RelativePosition);
            Assert.Empty(intersection.ContactNodes);
            Assert.Equal(2, intersection.IntersectionPoints.Length);

            var intersectionPoints = intersection.IntersectionPoints.Select(
                natural => element.StandardInterpolation.TransformNaturalToCartesian(element.Nodes, natural));
            CartesianPoint leftPoint = intersectionPoints.OrderBy(p => p.X).First();
            CartesianPoint rightPoint = intersectionPoints.OrderBy(p => p.X).Last();
            int precision = 10;
            Assert.Equal(2.0, leftPoint.X, precision);
            Assert.Equal(0.0, leftPoint.Y, precision);
            Assert.Equal(3.0, rightPoint.X, precision);
            Assert.Equal(1.0, rightPoint.Y, precision);
        }

        [Fact]
        public static void TestTangentElementAtEdge()
        {
            //            |
            //            |
            // ------------    
            // |          |    
            // |          |    
            // ------------    
            //            |
            //            |
            //

            IXFiniteElement element = CreateElement();
            var curve = new LineSegment2D(new CartesianPoint(4, 2), new CartesianPoint(4, -1), 1.0);

            CurveElementIntersection intersection = curve.IntersectElement(element);
            Assert.Equal(RelativePositionCurveElement.TangentAlongElementEdge, intersection.RelativePosition);
            Assert.Equal(2, intersection.IntersectionPoints.Length);
            Assert.Equal(2, intersection.ContactNodes.Length);

            XNode topNode = intersection.ContactNodes.OrderBy(n => n.Y).Last();
            XNode bottomNode = intersection.ContactNodes.OrderBy(n => n.Y).First();
            Assert.Equal(4.0, topNode.X);
            Assert.Equal(1.0, topNode.Y);
            Assert.Equal(4.0, bottomNode.X);
            Assert.Equal(0.0, bottomNode.Y);
        }

        //TODO: This test makes the Test Explorer hang indefinitely, thus I hace no way to actually debug it. I suspect it happens due to the tangent at 1 node
        //[Fact]
        public static void TestTangentElementAtNode()
        {
            //          \  
            //           \
            // ------------    
            // |          |\    
            // |          | \   
            // ------------  \  
            //

            IXFiniteElement element = CreateElement();
            var curve = new LineSegment2D(new CartesianPoint(6, 0), new CartesianPoint(2, 2), 1.0);

            CurveElementIntersection intersection = curve.IntersectElement(element);
            Assert.Equal(RelativePositionCurveElement.TangentAtSingleNode, intersection.RelativePosition);
            Assert.Single(intersection.IntersectionPoints);
            Assert.Single(intersection.ContactNodes);
            Assert.Equal(4.0, intersection.ContactNodes[0].X);
            Assert.Equal(1.0, intersection.ContactNodes[0].Y);
        }

        private static IXFiniteElement CreateElement()
        {
            var factory = new XThermalElement2DFactory(null, 0.0, null, 0);
            var nodes = new XNode[]
            {
                new XNode(0, 0.0, 0.0),
                new XNode(0, 4.0, 0.0),
                new XNode(0, 4.0, 1.0),
                new XNode(0, 0.0, 1.0)
            };
            return factory.CreateElement(0, CellType.Quad4, nodes);
        }
    }
}
