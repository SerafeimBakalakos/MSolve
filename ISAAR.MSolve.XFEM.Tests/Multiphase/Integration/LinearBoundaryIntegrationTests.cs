using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;
using ISAAR.MSolve.XFEM.Multiphase.Integration;
using Xunit;
using static ISAAR.MSolve.XFEM.Tests.Multiphase.Integration.BenchmarkDomain;

namespace ISAAR.MSolve.XFEM.Tests.Multiphase.Integration
{
    public static class LinearBoundaryIntegrationTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public static void TestIntegrationConstantFunc(int numGaussPoints)
        {
            var func = new BoundaryConstantFunction(-3.0);
            TestIntegration(func, numGaussPoints);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public static void TestIntegrationLinearFunc(int numGaussPoints)
        {
            var func = new BoundaryLinearFunction();
            TestIntegration(func, numGaussPoints);
        }

        private static void TestIntegration(IBenchmarkBoundaryFunction func, int numGaussPoints)
        {
            IXFiniteElement element = new BenchmarkDomain(GeometryType.Quad).Element;

            // Define segment along which we will integrate
            var naturalA = new NaturalPoint(-1, 0);
            var naturalB = new NaturalPoint(+1, 0);
            CartesianPoint cartesianA = element.InterpolationStandard.TransformNaturalToCartesian(element.Nodes, naturalA);
            CartesianPoint cartesianB = element.InterpolationStandard.TransformNaturalToCartesian(element.Nodes, naturalB);
            var intersection = new CurveElementIntersection(RelativePositionCurveElement.Intersection,
                new NaturalPoint[] { naturalA, naturalB });

            // Integrate
            var integration = new LinearBoundaryIntegration(GaussLegendre1D.GetQuadratureWithOrder(numGaussPoints));
            IReadOnlyList<GaussPoint> integrationPoints = integration.GenerateIntegrationPoints(element, intersection);
            double integral = Utilities.CalcIntegral(integrationPoints, element, func);

            // Check
            double expectedIntegral = func.GetExpectedIntegral(cartesianA, cartesianB);
            Assert.Equal(expectedIntegral, integral, 4);
        }
    }
}
