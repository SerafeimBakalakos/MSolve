using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Entities;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;
using Xunit;

namespace ISAAR.MSolve.XFEM.Tests.Multiphase.Geometry
{
    public static class LinearSegmentMeshInteractionTests
    {
        private static readonly IMeshTolerance meshTolerance = new UsedDefinedMeshTolerance(1.0, 1E-7);

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

            CurveElementIntersection intersection = curve.IntersectElement(element, meshTolerance);
            Assert.Equal(RelativePositionCurveElement.Disjoint, intersection.RelativePosition);
            Assert.Empty(intersection.IntersectionPoints);
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

            CurveElementIntersection intersection = curve.IntersectElement(element, meshTolerance);
            Assert.Equal(RelativePositionCurveElement.Disjoint, intersection.RelativePosition);
            Assert.Empty(intersection.IntersectionPoints);
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

            CurveElementIntersection intersection = curve.IntersectElement(element, meshTolerance);
            Assert.Equal(RelativePositionCurveElement.Intersection, intersection.RelativePosition);
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

            CurveElementIntersection intersection = curve.IntersectElement(element, meshTolerance);
            Assert.Equal(RelativePositionCurveElement.Intersection, intersection.RelativePosition);
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

            CurveElementIntersection intersection = curve.IntersectElement(element, meshTolerance);
            Assert.Equal(RelativePositionCurveElement.Intersection, intersection.RelativePosition);
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

            CurveElementIntersection intersection = curve.IntersectElement(element, meshTolerance);
            Assert.Equal(RelativePositionCurveElement.Intersection, intersection.RelativePosition);
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


            CurveElementIntersection intersection = curve.IntersectElement(element, meshTolerance);
            Assert.Equal(RelativePositionCurveElement.Tangent, intersection.RelativePosition);
            Assert.Equal(2, intersection.IntersectionPoints.Length);

            var intersectionPoints = intersection.IntersectionPoints.Select(
                natural => element.StandardInterpolation.TransformNaturalToCartesian(element.Nodes, natural));
            CartesianPoint topNode = intersectionPoints.OrderBy(p => p.Y).Last();
            CartesianPoint bottomNode = intersectionPoints.OrderBy(p => p.Y).First();
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

            CurveElementIntersection intersection = curve.IntersectElement(element, meshTolerance);
            Assert.Equal(RelativePositionCurveElement.Tangent, intersection.RelativePosition);
            Assert.Single(intersection.IntersectionPoints);

            CartesianPoint P = element.StandardInterpolation.TransformNaturalToCartesian(
                element.Nodes, intersection.IntersectionPoints[0]);
            Assert.Equal(4.0, P.X);
            Assert.Equal(1.0, P.Y);
        }

        private static IXFiniteElement CreateElement()
        {
            var nodes = new XNode[]
            {
                new XNode(0, 0.0, 0.0),
                new XNode(0, 4.0, 0.0),
                new XNode(0, 4.0, 1.0),
                new XNode(0, 0.0, 1.0)
            };
            return new MockQuad4(0, nodes);
        }
    }
}
