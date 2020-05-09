using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Entities;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Integration;
using Xunit;
using static ISAAR.MSolve.XFEM_OLD.Tests.Multiphase.Integration.BenchmarkDomain;

namespace ISAAR.MSolve.XFEM_OLD.Tests.Multiphase.Integration
{
    public static class IntegrationWithSubtrianglesTests
    {
        [Theory]
        [InlineData(GeometryType.Natural, 1)]
        [InlineData(GeometryType.Natural, 3)]
        [InlineData(GeometryType.Natural, 4)]
        [InlineData(GeometryType.Natural, 6)]
        [InlineData(GeometryType.Rectangle, 1)]
        [InlineData(GeometryType.Rectangle, 3)]
        [InlineData(GeometryType.Rectangle, 4)]
        [InlineData(GeometryType.Rectangle, 6)]
        [InlineData(GeometryType.Quad, 1)]
        [InlineData(GeometryType.Quad, 3)]
        [InlineData(GeometryType.Quad, 4)]
        [InlineData(GeometryType.Quad, 6)]
        public static void TestIntegrationConstantFunc(GeometryType geometryType, int numPointsPerTriangle)
        {
            var func = new ConstantFunction();
            TestIntegration(func, geometryType, numPointsPerTriangle);
        }

        [Theory]
        [InlineData(GeometryType.Natural, 1)] //TODO: Why are these enough integration points?
        [InlineData(GeometryType.Natural, 3)]
        [InlineData(GeometryType.Natural, 4)]
        [InlineData(GeometryType.Natural, 6)]
        [InlineData(GeometryType.Rectangle, 1)] //TODO: Why are these enough integration points?
        [InlineData(GeometryType.Rectangle, 3)]
        [InlineData(GeometryType.Rectangle, 4)]
        [InlineData(GeometryType.Rectangle, 6)]
        public static void TestIntegrationLinearFunc(GeometryType geometryType, int numPointsPerTriangle)
        {
            var func = new LinearFunction();
            TestIntegration(func, geometryType, numPointsPerTriangle);
        }

        [Theory]
        [InlineData(GeometryType.Natural, 1)]
        [InlineData(GeometryType.Natural, 3)]
        [InlineData(GeometryType.Natural, 4)]
        [InlineData(GeometryType.Natural, 6)]
        [InlineData(GeometryType.Rectangle, 1)]
        [InlineData(GeometryType.Rectangle, 3)]
        [InlineData(GeometryType.Rectangle, 4)]
        [InlineData(GeometryType.Rectangle, 6)]
        [InlineData(GeometryType.Quad, 1)]
        [InlineData(GeometryType.Quad, 3)]
        [InlineData(GeometryType.Quad, 4)]
        [InlineData(GeometryType.Quad, 6)]
        public static void TestIntegrationPieceWiseConstant2Func(GeometryType geometryType, int numPointsPerTriangle)
        {
            var func = new PiecewiseConstant2Function();
            TestIntegration(func, geometryType, numPointsPerTriangle);
        }

        [Theory]
        [InlineData(GeometryType.Natural, 1)]
        [InlineData(GeometryType.Natural, 3)]
        [InlineData(GeometryType.Natural, 4)]
        [InlineData(GeometryType.Natural, 6)]
        [InlineData(GeometryType.Rectangle, 1)]
        [InlineData(GeometryType.Rectangle, 3)]
        [InlineData(GeometryType.Rectangle, 4)]
        [InlineData(GeometryType.Rectangle, 6)]
        [InlineData(GeometryType.Quad, 1)]
        [InlineData(GeometryType.Quad, 3)]
        [InlineData(GeometryType.Quad, 4)]
        [InlineData(GeometryType.Quad, 6)]
        public static void TestIntegrationPieceWiseConstant4Func(GeometryType geometryType, int numPointsPerTriangle)
        {
            var func = new PiecewiseConstant4Function();
            TestIntegration(func, geometryType, numPointsPerTriangle);
        }

        [Theory]
        //[InlineData(GeometryType.Natural, 1)] // Not enough integration points
        [InlineData(GeometryType.Natural, 3)]
        [InlineData(GeometryType.Natural, 4)]
        [InlineData(GeometryType.Natural, 6)]
        //[InlineData(GeometryType.Rectangle, 1)] // Not enough integration points
        [InlineData(GeometryType.Rectangle, 3)]
        [InlineData(GeometryType.Rectangle, 4)]
        [InlineData(GeometryType.Rectangle, 6)]
        public static void TestIntegrationPieceWiseLinearFunc(GeometryType geometryType, int numPointsPerTriangle)
        {
            var func = new PiecewiseLinear2Function();
            TestIntegration(func, geometryType, numPointsPerTriangle);
        }

        private static void TestIntegration(IBenchmarkVolumeFunction func, GeometryType geometryType, int numPointsPerTriangle)
        {
            var domain = new BenchmarkDomain(geometryType);
            IXFiniteElement element = domain.Element;
            double expectedIntegral = func.GetExpectedIntegral(geometryType);

            // With 2 phases, the element will be identified as enriched and subrectangles will be used
            //var phase1 = new ConvexPhase(1);
            //var phase2 = new ConvexPhase(2);
            element.Phases.Add(new ConvexPhase(1));
            element.Phases.Add(new ConvexPhase(2));

            double elementSize = element.Nodes[0].CalculateDistanceFrom(element.Nodes[1]);
            var meshTolerance = new UserDefinedMeshTolerance(elementSize);
            CurveElementIntersection[] intersections = func.GetIntersectionSegments();
            var triangulator = new ConformingTriangulator();
            ElementSubtriangle[] subtriangles = triangulator.FindConformingMesh(element, intersections, meshTolerance);

            var quadrature = GaussLegendre2D.GetQuadratureWithOrder(2, 2);
            TriangleQuadratureSymmetricGaussian triangleQuadrature;
            if (numPointsPerTriangle == 1) triangleQuadrature = TriangleQuadratureSymmetricGaussian.Order1Point1;
            else if (numPointsPerTriangle == 3) triangleQuadrature = TriangleQuadratureSymmetricGaussian.Order2Points3;
            else if (numPointsPerTriangle == 4) triangleQuadrature = TriangleQuadratureSymmetricGaussian.Order3Points4;
            else if (numPointsPerTriangle == 6) triangleQuadrature = TriangleQuadratureSymmetricGaussian.Order4Points6;
            else throw new NotImplementedException();

            var volumeIntegration = new IntegrationWithConformingSubtriangles2D(quadrature, triangleQuadrature,
                e => subtriangles);
            IReadOnlyList<GaussPoint> gaussPoints = volumeIntegration.GenerateIntegrationPoints(element);

            double integral = Utilities.CalcIntegral(gaussPoints, domain, func);
            Assert.Equal(expectedIntegral, integral, 4);
        }
    }
}
