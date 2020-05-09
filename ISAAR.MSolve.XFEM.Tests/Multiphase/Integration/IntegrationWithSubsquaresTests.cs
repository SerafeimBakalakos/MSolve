using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.FEM.Interpolation.Jacobians;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Entities;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Integration;
using Xunit;
using static ISAAR.MSolve.XFEM_OLD.Tests.Multiphase.Integration.BenchmarkDomain;

namespace ISAAR.MSolve.XFEM_OLD.Tests.Multiphase.Integration
{
    public static class IntegrationWithSubsquaresTests
    {
        [Theory]
        [InlineData(GeometryType.Natural, 1, 1)]
        [InlineData(GeometryType.Natural, 1, 2)]
        [InlineData(GeometryType.Natural, 1, 3)]
        [InlineData(GeometryType.Natural, 2, 1)]
        [InlineData(GeometryType.Natural, 2, 2)]
        [InlineData(GeometryType.Natural, 2, 3)]
        [InlineData(GeometryType.Natural, 4, 1)]
        [InlineData(GeometryType.Natural, 4, 2)]
        [InlineData(GeometryType.Natural, 4, 3)]
        [InlineData(GeometryType.Natural, 8, 1)]
        [InlineData(GeometryType.Natural, 8, 2)]
        [InlineData(GeometryType.Natural, 8, 3)]
        [InlineData(GeometryType.Rectangle, 1, 1)]
        [InlineData(GeometryType.Rectangle, 1, 2)]
        [InlineData(GeometryType.Rectangle, 1, 3)]
        [InlineData(GeometryType.Rectangle, 2, 1)]
        [InlineData(GeometryType.Rectangle, 2, 2)]
        [InlineData(GeometryType.Rectangle, 2, 3)]
        [InlineData(GeometryType.Rectangle, 4, 1)]
        [InlineData(GeometryType.Rectangle, 4, 2)]
        [InlineData(GeometryType.Rectangle, 4, 3)]
        [InlineData(GeometryType.Rectangle, 8, 1)]
        [InlineData(GeometryType.Rectangle, 8, 2)]
        [InlineData(GeometryType.Rectangle, 8, 3)]
        [InlineData(GeometryType.Quad, 1, 1)]
        [InlineData(GeometryType.Quad, 1, 2)]
        [InlineData(GeometryType.Quad, 1, 3)]
        [InlineData(GeometryType.Quad, 2, 1)]
        [InlineData(GeometryType.Quad, 2, 2)]
        [InlineData(GeometryType.Quad, 2, 3)]
        [InlineData(GeometryType.Quad, 4, 1)]
        [InlineData(GeometryType.Quad, 4, 2)]
        [InlineData(GeometryType.Quad, 4, 3)]
        [InlineData(GeometryType.Quad, 8, 1)]
        [InlineData(GeometryType.Quad, 8, 2)]
        [InlineData(GeometryType.Quad, 8, 3)]
        public static void TestIntegrationConstantFunc(GeometryType geometryType, int numSquaresPerAxis, int numPointsPerAxis)
        {
            var domain = new BenchmarkDomain(geometryType);
            IXFiniteElement element = domain.Element;
            var func = new ConstantFunction();
            double expectedIntegral = func.GetExpectedIntegral(geometryType);

            // With 2 phases, the element will be identified as enriched and subrectangles will be used
            element.Phases.Add(new ConvexPhase(1));
            element.Phases.Add(new ConvexPhase(2));

            var quadrature = GaussLegendre2D.GetQuadratureWithOrder(numPointsPerAxis, numPointsPerAxis);
            var volumeIntegration = new IntegrationWithNonConformingSubsquares2D(quadrature, numSquaresPerAxis, quadrature);
            IReadOnlyList<GaussPoint> gaussPoints = volumeIntegration.GenerateIntegrationPoints(element);

            double integral = Utilities.CalcIntegral(gaussPoints, domain, func);
            Assert.Equal(expectedIntegral, integral, 4);
        }

        [Theory]
        [InlineData(GeometryType.Natural, 1, 1)]
        [InlineData(GeometryType.Natural, 1, 2)]
        [InlineData(GeometryType.Natural, 1, 3)]
        [InlineData(GeometryType.Natural, 2, 1)]
        [InlineData(GeometryType.Natural, 2, 2)]
        [InlineData(GeometryType.Natural, 2, 3)]
        [InlineData(GeometryType.Natural, 4, 1)]
        [InlineData(GeometryType.Natural, 4, 2)]
        [InlineData(GeometryType.Natural, 4, 3)]
        [InlineData(GeometryType.Natural, 8, 1)]
        [InlineData(GeometryType.Natural, 8, 2)]
        [InlineData(GeometryType.Natural, 8, 3)]
        [InlineData(GeometryType.Rectangle, 1, 1)]
        [InlineData(GeometryType.Rectangle, 1, 2)]
        [InlineData(GeometryType.Rectangle, 1, 3)]
        [InlineData(GeometryType.Rectangle, 2, 1)]
        [InlineData(GeometryType.Rectangle, 2, 2)]
        [InlineData(GeometryType.Rectangle, 2, 3)]
        [InlineData(GeometryType.Rectangle, 4, 1)]
        [InlineData(GeometryType.Rectangle, 4, 2)]
        [InlineData(GeometryType.Rectangle, 4, 3)]
        [InlineData(GeometryType.Rectangle, 8, 1)]
        [InlineData(GeometryType.Rectangle, 8, 2)]
        [InlineData(GeometryType.Rectangle, 8, 3)]
        public static void TestIntegrationLinearFunc(GeometryType geometryType, int numSquaresPerAxis, int numPointsPerAxis)
        {
            var domain = new BenchmarkDomain(geometryType);
            IXFiniteElement element = domain.Element;
            var func = new LinearFunction();
            double expectedIntegral = func.GetExpectedIntegral(geometryType);

            // With 2 phases, the element will be identified as enriched and subrectangles will be used
            element.Phases.Add(new ConvexPhase(1));
            element.Phases.Add(new ConvexPhase(2));

            var quadrature = GaussLegendre2D.GetQuadratureWithOrder(numPointsPerAxis, numPointsPerAxis);
            var volumeIntegration = new IntegrationWithNonConformingSubsquares2D(quadrature, numSquaresPerAxis, quadrature);
            IReadOnlyList<GaussPoint> gaussPoints = volumeIntegration.GenerateIntegrationPoints(element);

            double integral = Utilities.CalcIntegral(gaussPoints, domain, func);
            Assert.Equal(expectedIntegral, integral, 4);
        }

        [Theory]
        [InlineData(GeometryType.Natural, 1, 1)]
        //[InlineData(GeometryType.Natural, 1, 2)] //TODO: This should fail, but it gives the correct result!
        [InlineData(GeometryType.Natural, 1, 3)]
        [InlineData(GeometryType.Natural, 2, 1)]
        [InlineData(GeometryType.Natural, 2, 2)]
        [InlineData(GeometryType.Natural, 2, 3)]
        [InlineData(GeometryType.Natural, 4, 1)]
        [InlineData(GeometryType.Natural, 4, 2)]
        [InlineData(GeometryType.Natural, 4, 3)]
        [InlineData(GeometryType.Natural, 8, 1)]
        [InlineData(GeometryType.Natural, 8, 2)]
        [InlineData(GeometryType.Natural, 8, 3)]
        [InlineData(GeometryType.Rectangle, 1, 1)]
        //[InlineData(GeometryType.Rectangle, 1, 2)] //TODO: This should fail, but it gives the correct result!
        [InlineData(GeometryType.Rectangle, 1, 3)]
        [InlineData(GeometryType.Rectangle, 2, 1)]
        [InlineData(GeometryType.Rectangle, 2, 2)]
        [InlineData(GeometryType.Rectangle, 2, 3)]
        [InlineData(GeometryType.Rectangle, 4, 1)]
        [InlineData(GeometryType.Rectangle, 4, 2)]
        [InlineData(GeometryType.Rectangle, 4, 3)]
        [InlineData(GeometryType.Rectangle, 8, 1)]
        [InlineData(GeometryType.Rectangle, 8, 2)]
        [InlineData(GeometryType.Rectangle, 8, 3)]
        [InlineData(GeometryType.Quad, 1, 1)]
        [InlineData(GeometryType.Quad, 1, 2)]
        [InlineData(GeometryType.Quad, 1, 3)]
        [InlineData(GeometryType.Quad, 2, 1)]
        [InlineData(GeometryType.Quad, 2, 2)]
        [InlineData(GeometryType.Quad, 2, 3)]
        [InlineData(GeometryType.Quad, 4, 1)]
        [InlineData(GeometryType.Quad, 4, 2)]
        [InlineData(GeometryType.Quad, 4, 3)]
        [InlineData(GeometryType.Quad, 8, 1)]
        [InlineData(GeometryType.Quad, 8, 2)]
        [InlineData(GeometryType.Quad, 8, 3)]
        public static void TestIntegrationPieceWiseConstant2Func(GeometryType geometryType, int numSquaresPerAxis, 
            int numPointsPerAxis)
        {
            var domain = new BenchmarkDomain(geometryType);
            IXFiniteElement element = domain.Element;
            var func = new PiecewiseConstant2Function();
            double expectedIntegral = func.GetExpectedIntegral(geometryType);

            // With 2 phases, the element will be identified as enriched and subrectangles will be used
            element.Phases.Add(new ConvexPhase(1));
            element.Phases.Add(new ConvexPhase(2));

            var quadrature = GaussLegendre2D.GetQuadratureWithOrder(numPointsPerAxis, numPointsPerAxis);
            var volumeIntegration = new IntegrationWithNonConformingSubsquares2D(quadrature, numSquaresPerAxis, quadrature);
            IReadOnlyList<GaussPoint> gaussPoints = volumeIntegration.GenerateIntegrationPoints(element);

            if (numSquaresPerAxis % 2 == 0)
            {
                double integral = Utilities.CalcIntegral(gaussPoints, domain, func);
                Assert.Equal(expectedIntegral, integral, 4);
            }
            else CheckIncorrectIntegration(domain, gaussPoints, func, expectedIntegral);
        }

        [Theory]
        [InlineData(GeometryType.Natural, 1, 1)]
        [InlineData(GeometryType.Natural, 1, 2)]
        [InlineData(GeometryType.Natural, 1, 3)]
        [InlineData(GeometryType.Natural, 2, 1)]
        //[InlineData(GeometryType.Natural, 2, 2)] //TODO: This should fail, but it gives the correct result!
        [InlineData(GeometryType.Natural, 2, 3)]
        [InlineData(GeometryType.Natural, 4, 1)]
        [InlineData(GeometryType.Natural, 4, 2)]
        [InlineData(GeometryType.Natural, 4, 3)]
        [InlineData(GeometryType.Natural, 8, 1)]
        [InlineData(GeometryType.Natural, 8, 2)]
        [InlineData(GeometryType.Natural, 8, 3)]
        [InlineData(GeometryType.Rectangle, 1, 1)]
        [InlineData(GeometryType.Rectangle, 1, 2)]
        [InlineData(GeometryType.Rectangle, 1, 3)]
        [InlineData(GeometryType.Rectangle, 2, 1)]
        //[InlineData(GeometryType.Rectangle, 2, 2)] //TODO: This should fail, but it gives the correct result!
        [InlineData(GeometryType.Rectangle, 2, 3)]
        [InlineData(GeometryType.Rectangle, 4, 1)]
        [InlineData(GeometryType.Rectangle, 4, 2)]
        [InlineData(GeometryType.Rectangle, 4, 3)]
        [InlineData(GeometryType.Rectangle, 8, 1)]
        [InlineData(GeometryType.Rectangle, 8, 2)]
        [InlineData(GeometryType.Rectangle, 8, 3)]
        [InlineData(GeometryType.Quad, 1, 1)]
        [InlineData(GeometryType.Quad, 1, 2)]
        [InlineData(GeometryType.Quad, 1, 3)]
        [InlineData(GeometryType.Quad, 2, 1)]
        [InlineData(GeometryType.Quad, 2, 2)]
        [InlineData(GeometryType.Quad, 2, 3)]
        [InlineData(GeometryType.Quad, 4, 1)]
        [InlineData(GeometryType.Quad, 4, 2)]
        [InlineData(GeometryType.Quad, 4, 3)]
        [InlineData(GeometryType.Quad, 8, 1)]
        [InlineData(GeometryType.Quad, 8, 2)]
        [InlineData(GeometryType.Quad, 8, 3)]
        public static void TestIntegrationPieceWiseConstant4Func(GeometryType geometryType, int numSquaresPerAxis,
            int numPointsPerAxis)
        {
            var domain = new BenchmarkDomain(geometryType);
            IXFiniteElement element = domain.Element;
            var func = new PiecewiseConstant4Function();
            double expectedIntegral = func.GetExpectedIntegral(geometryType);

            // With 2 phases, the element will be identified as enriched and subrectangles will be used
            element.Phases.Add(new ConvexPhase(1));
            element.Phases.Add(new ConvexPhase(2));

            var quadrature = GaussLegendre2D.GetQuadratureWithOrder(numPointsPerAxis, numPointsPerAxis);
            var volumeIntegration = new IntegrationWithNonConformingSubsquares2D(quadrature, numSquaresPerAxis, quadrature);
            IReadOnlyList<GaussPoint> gaussPoints = volumeIntegration.GenerateIntegrationPoints(element);

            if (numSquaresPerAxis % 4 == 0)
            {
                double integral = Utilities.CalcIntegral(gaussPoints, domain, func);
                Assert.Equal(expectedIntegral, integral, 4);
            }
            else CheckIncorrectIntegration(domain, gaussPoints, func, expectedIntegral);
        }

        [Theory]
        [InlineData(GeometryType.Natural, 1, 1)]
        [InlineData(GeometryType.Natural, 1, 2)]
        [InlineData(GeometryType.Natural, 1, 3)]
        [InlineData(GeometryType.Natural, 2, 1)]
        //[InlineData(GeometryType.Natural, 2, 2)] //TODO: This should fail, but it gives the correct result!
        [InlineData(GeometryType.Natural, 2, 3)]
        [InlineData(GeometryType.Natural, 4, 1)]
        [InlineData(GeometryType.Natural, 4, 2)]
        [InlineData(GeometryType.Natural, 4, 3)]
        [InlineData(GeometryType.Natural, 8, 1)]
        [InlineData(GeometryType.Natural, 8, 2)]
        [InlineData(GeometryType.Natural, 8, 3)]
        [InlineData(GeometryType.Rectangle, 1, 1)]
        [InlineData(GeometryType.Rectangle, 1, 2)]
        [InlineData(GeometryType.Rectangle, 1, 3)]
        [InlineData(GeometryType.Rectangle, 2, 1)]
        [InlineData(GeometryType.Rectangle, 2, 2)]
        [InlineData(GeometryType.Rectangle, 2, 3)]
        [InlineData(GeometryType.Rectangle, 4, 1)]
        [InlineData(GeometryType.Rectangle, 4, 2)]
        [InlineData(GeometryType.Rectangle, 4, 3)]
        [InlineData(GeometryType.Rectangle, 8, 1)]
        [InlineData(GeometryType.Rectangle, 8, 2)]
        [InlineData(GeometryType.Rectangle, 8, 3)]
        public static void TestIntegrationPieceWiseLinear2Func(GeometryType geometryType, int numSquaresPerAxis,
            int numPointsPerAxis)
        {
            var domain = new BenchmarkDomain(geometryType);
            IXFiniteElement element = domain.Element;
            var func = new PiecewiseLinear2Function();
            double expectedIntegral = func.GetExpectedIntegral(geometryType);

            // With 2 phases, the element will be identified as enriched and subrectangles will be used
            element.Phases.Add(new ConvexPhase(1));
            element.Phases.Add(new ConvexPhase(2));

            var quadrature = GaussLegendre2D.GetQuadratureWithOrder(numPointsPerAxis, numPointsPerAxis);
            var volumeIntegration = new IntegrationWithNonConformingSubsquares2D(quadrature, numSquaresPerAxis, quadrature);
            IReadOnlyList<GaussPoint> gaussPoints = volumeIntegration.GenerateIntegrationPoints(element);

            if (numSquaresPerAxis % 4 == 0)
            {
                double integral = Utilities.CalcIntegral(gaussPoints, domain, func);
                Assert.Equal(expectedIntegral, integral, 4);
            }
            else CheckIncorrectIntegration(domain, gaussPoints, func, expectedIntegral);
        }

        

        private static void CheckIncorrectIntegration(BenchmarkDomain domain, IReadOnlyList<GaussPoint> gaussPoints,
            IBenchmarkVolumeFunction func, double expectedIntegral)
        {
            try
            {
                double integral = Utilities.CalcIntegral(gaussPoints, domain, func);
                Assert.NotEqual(expectedIntegral, integral, 4);
            }
            catch (Exception ex)
            {
                bool gaussPointInInvalidRegion = false;
                foreach (GaussPoint point in gaussPoints)
                {
                    if (!func.IsInValidRegion(point))
                    {
                        gaussPointInInvalidRegion = true;
                        break;
                    }
                }
                if (gaussPointInInvalidRegion)
                {
                    Assert.True(true, "Correctly caught that there are gauss points on the boundary");
                }
                else throw ex;
            }
        }
    }
}
