using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.FEM.Interpolation.Jacobians;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Entities;
using ISAAR.MSolve.XFEM.Multiphase.Integration;
using Xunit;
using static ISAAR.MSolve.XFEM.Tests.Multiphase.Integration.IntegrationBenchmarks;

namespace ISAAR.MSolve.XFEM.Tests.Multiphase.Integration
{
    public static class IntegrationWithSubsquaresTests
    {
        [Theory]
        [InlineData(ElementType.Natural, 1, 1)]
        [InlineData(ElementType.Natural, 1, 2)]
        [InlineData(ElementType.Natural, 1, 3)]
        [InlineData(ElementType.Natural, 2, 1)]
        [InlineData(ElementType.Natural, 2, 2)]
        [InlineData(ElementType.Natural, 2, 3)]
        [InlineData(ElementType.Natural, 4, 1)]
        [InlineData(ElementType.Natural, 4, 2)]
        [InlineData(ElementType.Natural, 4, 3)]
        [InlineData(ElementType.Natural, 8, 1)]
        [InlineData(ElementType.Natural, 8, 2)]
        [InlineData(ElementType.Natural, 8, 3)]
        [InlineData(ElementType.Rectangle, 1, 1)]
        [InlineData(ElementType.Rectangle, 1, 2)]
        [InlineData(ElementType.Rectangle, 1, 3)]
        [InlineData(ElementType.Rectangle, 2, 1)]
        [InlineData(ElementType.Rectangle, 2, 2)]
        [InlineData(ElementType.Rectangle, 2, 3)]
        [InlineData(ElementType.Rectangle, 4, 1)]
        [InlineData(ElementType.Rectangle, 4, 2)]
        [InlineData(ElementType.Rectangle, 4, 3)]
        [InlineData(ElementType.Rectangle, 8, 1)]
        [InlineData(ElementType.Rectangle, 8, 2)]
        [InlineData(ElementType.Rectangle, 8, 3)]
        [InlineData(ElementType.Quad, 1, 1)]
        [InlineData(ElementType.Quad, 1, 2)]
        [InlineData(ElementType.Quad, 1, 3)]
        [InlineData(ElementType.Quad, 2, 1)]
        [InlineData(ElementType.Quad, 2, 2)]
        [InlineData(ElementType.Quad, 2, 3)]
        [InlineData(ElementType.Quad, 4, 1)]
        [InlineData(ElementType.Quad, 4, 2)]
        [InlineData(ElementType.Quad, 4, 3)]
        [InlineData(ElementType.Quad, 8, 1)]
        [InlineData(ElementType.Quad, 8, 2)]
        [InlineData(ElementType.Quad, 8, 3)]
        public static void TestIntegrationConstantFunc(ElementType elementType, int numSquaresPerAxis, int numPointsPerAxis)
        {
            MockQuad4 element = SetupElement(elementType);
            Func<IXFiniteElement, NaturalPoint, double> func = SetupFunction(FunctionType.One);
            double expectedIntegral = SetupExpectedIntegral(elementType, FunctionType.One);

            // With 2 phases, the element will be identified as enriched and subrectangles will be used
            element.Phases.Add(new ConvexPhase(1));
            element.Phases.Add(new ConvexPhase(2));

            var quadrature = GaussLegendre2D.GetQuadratureWithOrder(numPointsPerAxis, numPointsPerAxis);
            var volumeIntegration = new IntegrationWithNonConformingSubsquares2D(quadrature, numSquaresPerAxis, quadrature);
            IReadOnlyList<GaussPoint> gaussPoints = volumeIntegration.GenerateIntegrationPoints(element);

            double integral = CalcIntegral(gaussPoints, element, func);
            Assert.Equal(expectedIntegral, integral, 4);
        }

        [Theory]
        [InlineData(ElementType.Natural, 1, 1)]
        [InlineData(ElementType.Natural, 1, 2)]
        [InlineData(ElementType.Natural, 1, 3)]
        [InlineData(ElementType.Natural, 2, 1)]
        [InlineData(ElementType.Natural, 2, 2)]
        [InlineData(ElementType.Natural, 2, 3)]
        [InlineData(ElementType.Natural, 4, 1)]
        [InlineData(ElementType.Natural, 4, 2)]
        [InlineData(ElementType.Natural, 4, 3)]
        [InlineData(ElementType.Natural, 8, 1)]
        [InlineData(ElementType.Natural, 8, 2)]
        [InlineData(ElementType.Natural, 8, 3)]
        [InlineData(ElementType.Rectangle, 1, 1)]
        [InlineData(ElementType.Rectangle, 1, 2)]
        [InlineData(ElementType.Rectangle, 1, 3)]
        [InlineData(ElementType.Rectangle, 2, 1)]
        [InlineData(ElementType.Rectangle, 2, 2)]
        [InlineData(ElementType.Rectangle, 2, 3)]
        [InlineData(ElementType.Rectangle, 4, 1)]
        [InlineData(ElementType.Rectangle, 4, 2)]
        [InlineData(ElementType.Rectangle, 4, 3)]
        [InlineData(ElementType.Rectangle, 8, 1)]
        [InlineData(ElementType.Rectangle, 8, 2)]
        [InlineData(ElementType.Rectangle, 8, 3)]
        public static void TestIntegrationLinearFunc(ElementType elementType, int numSquaresPerAxis, int numPointsPerAxis)
        {
            MockQuad4 element = SetupElement(elementType);
            Func<IXFiniteElement, NaturalPoint, double> func = SetupFunction(FunctionType.Linear);
            double expectedIntegral = SetupExpectedIntegral(elementType, FunctionType.Linear);

            // With 2 phases, the element will be identified as enriched and subrectangles will be used
            element.Phases.Add(new ConvexPhase(1));
            element.Phases.Add(new ConvexPhase(2));

            var quadrature = GaussLegendre2D.GetQuadratureWithOrder(numPointsPerAxis, numPointsPerAxis);
            var volumeIntegration = new IntegrationWithNonConformingSubsquares2D(quadrature, numSquaresPerAxis, quadrature);
            IReadOnlyList<GaussPoint> gaussPoints = volumeIntegration.GenerateIntegrationPoints(element);

            double integral = CalcIntegral(gaussPoints, element, func);
            Assert.Equal(expectedIntegral, integral, 4);
        }

        [Theory]
        [InlineData(ElementType.Natural, 1, 1)]
        //[InlineData(ElementType.Natural, 1, 2)] //TODO: This should fail, but it gives the correct result!
        [InlineData(ElementType.Natural, 1, 3)]
        [InlineData(ElementType.Natural, 2, 1)]
        [InlineData(ElementType.Natural, 2, 2)]
        [InlineData(ElementType.Natural, 2, 3)]
        [InlineData(ElementType.Natural, 4, 1)]
        [InlineData(ElementType.Natural, 4, 2)]
        [InlineData(ElementType.Natural, 4, 3)]
        [InlineData(ElementType.Natural, 8, 1)]
        [InlineData(ElementType.Natural, 8, 2)]
        [InlineData(ElementType.Natural, 8, 3)]
        [InlineData(ElementType.Rectangle, 1, 1)]
        //[InlineData(ElementType.Rectangle, 1, 2)] //TODO: This should fail, but it gives the correct result!
        [InlineData(ElementType.Rectangle, 1, 3)]
        [InlineData(ElementType.Rectangle, 2, 1)]
        [InlineData(ElementType.Rectangle, 2, 2)]
        [InlineData(ElementType.Rectangle, 2, 3)]
        [InlineData(ElementType.Rectangle, 4, 1)]
        [InlineData(ElementType.Rectangle, 4, 2)]
        [InlineData(ElementType.Rectangle, 4, 3)]
        [InlineData(ElementType.Rectangle, 8, 1)]
        [InlineData(ElementType.Rectangle, 8, 2)]
        [InlineData(ElementType.Rectangle, 8, 3)]
        [InlineData(ElementType.Quad, 1, 1)]
        [InlineData(ElementType.Quad, 1, 2)]
        [InlineData(ElementType.Quad, 1, 3)]
        [InlineData(ElementType.Quad, 2, 1)]
        [InlineData(ElementType.Quad, 2, 2)]
        [InlineData(ElementType.Quad, 2, 3)]
        [InlineData(ElementType.Quad, 4, 1)]
        [InlineData(ElementType.Quad, 4, 2)]
        [InlineData(ElementType.Quad, 4, 3)]
        [InlineData(ElementType.Quad, 8, 1)]
        [InlineData(ElementType.Quad, 8, 2)]
        [InlineData(ElementType.Quad, 8, 3)]
        public static void TestIntegrationPieceWiseConstant2Func(ElementType elementType, int numSquaresPerAxis, 
            int numPointsPerAxis)
        {
            MockQuad4 element = SetupElement(elementType);
            Func<IXFiniteElement, NaturalPoint, double> func = SetupFunction(FunctionType.PiecewiseConstant2);
            double expectedIntegral = SetupExpectedIntegral(elementType, FunctionType.PiecewiseConstant2);

            // With 2 phases, the element will be identified as enriched and subrectangles will be used
            element.Phases.Add(new ConvexPhase(1));
            element.Phases.Add(new ConvexPhase(2));

            var quadrature = GaussLegendre2D.GetQuadratureWithOrder(numPointsPerAxis, numPointsPerAxis);
            var volumeIntegration = new IntegrationWithNonConformingSubsquares2D(quadrature, numSquaresPerAxis, quadrature);
            IReadOnlyList<GaussPoint> gaussPoints = volumeIntegration.GenerateIntegrationPoints(element);

            if (numSquaresPerAxis % 2 == 0)
            {
                double integral = CalcIntegral(gaussPoints, element, func);
                Assert.Equal(expectedIntegral, integral, 4);
            }
            else
            {
                Func<GaussPoint, bool> isPointInValidRegion = p => (p.Xi < 0.0) || (p.Xi > 0.0); //TODO: This should be provided by the function
                CheckIncorrectIntegration(gaussPoints, element, func, expectedIntegral, isPointInValidRegion);
            }
        }

        [Theory]
        [InlineData(ElementType.Natural, 1, 1)]
        [InlineData(ElementType.Natural, 1, 2)]
        [InlineData(ElementType.Natural, 1, 3)]
        [InlineData(ElementType.Natural, 2, 1)]
        //[InlineData(ElementType.Natural, 2, 2)] //TODO: This should fail, but it gives the correct result!
        [InlineData(ElementType.Natural, 2, 3)]
        [InlineData(ElementType.Natural, 4, 1)]
        [InlineData(ElementType.Natural, 4, 2)]
        [InlineData(ElementType.Natural, 4, 3)]
        [InlineData(ElementType.Natural, 8, 1)]
        [InlineData(ElementType.Natural, 8, 2)]
        [InlineData(ElementType.Natural, 8, 3)]
        [InlineData(ElementType.Rectangle, 1, 1)]
        [InlineData(ElementType.Rectangle, 1, 2)]
        [InlineData(ElementType.Rectangle, 1, 3)]
        [InlineData(ElementType.Rectangle, 2, 1)]
        //[InlineData(ElementType.Rectangle, 2, 2)] //TODO: This should fail, but it gives the correct result!
        [InlineData(ElementType.Rectangle, 2, 3)]
        [InlineData(ElementType.Rectangle, 4, 1)]
        [InlineData(ElementType.Rectangle, 4, 2)]
        [InlineData(ElementType.Rectangle, 4, 3)]
        [InlineData(ElementType.Rectangle, 8, 1)]
        [InlineData(ElementType.Rectangle, 8, 2)]
        [InlineData(ElementType.Rectangle, 8, 3)]
        [InlineData(ElementType.Quad, 1, 1)]
        [InlineData(ElementType.Quad, 1, 2)]
        [InlineData(ElementType.Quad, 1, 3)]
        [InlineData(ElementType.Quad, 2, 1)]
        [InlineData(ElementType.Quad, 2, 2)]
        [InlineData(ElementType.Quad, 2, 3)]
        [InlineData(ElementType.Quad, 4, 1)]
        [InlineData(ElementType.Quad, 4, 2)]
        [InlineData(ElementType.Quad, 4, 3)]
        [InlineData(ElementType.Quad, 8, 1)]
        [InlineData(ElementType.Quad, 8, 2)]
        [InlineData(ElementType.Quad, 8, 3)]
        public static void TestIntegrationPieceWiseConstant4Func(ElementType elementType, int numSquaresPerAxis,
            int numPointsPerAxis)
        {
            MockQuad4 element = SetupElement(elementType);
            Func<IXFiniteElement, NaturalPoint, double> func = SetupFunction(FunctionType.PiecewiseConstant4);
            double expectedIntegral = SetupExpectedIntegral(elementType, FunctionType.PiecewiseConstant4);

            // With 2 phases, the element will be identified as enriched and subrectangles will be used
            element.Phases.Add(new ConvexPhase(1));
            element.Phases.Add(new ConvexPhase(2));

            var quadrature = GaussLegendre2D.GetQuadratureWithOrder(numPointsPerAxis, numPointsPerAxis);
            var volumeIntegration = new IntegrationWithNonConformingSubsquares2D(quadrature, numSquaresPerAxis, quadrature);
            IReadOnlyList<GaussPoint> gaussPoints = volumeIntegration.GenerateIntegrationPoints(element);

            if (numSquaresPerAxis % 4 == 0)
            {
                double integral = CalcIntegral(gaussPoints, element, func);
                Assert.Equal(expectedIntegral, integral, 4);
            }
            else
            {
                Func<GaussPoint, bool> isPointInValidRegion = p =>
                {
                    return ((p.Xi < -0.5) || (p.Xi > -0.5)) && ((p.Eta < 0.0) || (p.Eta > 0.0));
                }; //TODO: This should be provided by the function
                CheckIncorrectIntegration(gaussPoints, element, func, expectedIntegral, isPointInValidRegion);
            }
        }

        [Theory]
        [InlineData(ElementType.Natural, 1, 1)]
        [InlineData(ElementType.Natural, 1, 2)]
        [InlineData(ElementType.Natural, 1, 3)]
        [InlineData(ElementType.Natural, 2, 1)]
        //[InlineData(ElementType.Natural, 2, 2)] //TODO: This should fail, but it gives the correct result!
        [InlineData(ElementType.Natural, 2, 3)]
        [InlineData(ElementType.Natural, 4, 1)]
        [InlineData(ElementType.Natural, 4, 2)]
        [InlineData(ElementType.Natural, 4, 3)]
        [InlineData(ElementType.Natural, 8, 1)]
        [InlineData(ElementType.Natural, 8, 2)]
        [InlineData(ElementType.Natural, 8, 3)]
        [InlineData(ElementType.Rectangle, 1, 1)]
        [InlineData(ElementType.Rectangle, 1, 2)]
        [InlineData(ElementType.Rectangle, 1, 3)]
        [InlineData(ElementType.Rectangle, 2, 1)]
        [InlineData(ElementType.Rectangle, 2, 2)]
        [InlineData(ElementType.Rectangle, 2, 3)]
        [InlineData(ElementType.Rectangle, 4, 1)]
        [InlineData(ElementType.Rectangle, 4, 2)]
        [InlineData(ElementType.Rectangle, 4, 3)]
        [InlineData(ElementType.Rectangle, 8, 1)]
        [InlineData(ElementType.Rectangle, 8, 2)]
        [InlineData(ElementType.Rectangle, 8, 3)]
        public static void TestIntegrationPieceWiseLinear2Func(ElementType elementType, int numSquaresPerAxis,
            int numPointsPerAxis)
        {
            MockQuad4 element = SetupElement(elementType);
            Func<IXFiniteElement, NaturalPoint, double> func = SetupFunction(FunctionType.PiecewiseLinear2);
            double expectedIntegral = SetupExpectedIntegral(elementType, FunctionType.PiecewiseLinear2);

            // With 2 phases, the element will be identified as enriched and subrectangles will be used
            element.Phases.Add(new ConvexPhase(1));
            element.Phases.Add(new ConvexPhase(2));

            var quadrature = GaussLegendre2D.GetQuadratureWithOrder(numPointsPerAxis, numPointsPerAxis);
            var volumeIntegration = new IntegrationWithNonConformingSubsquares2D(quadrature, numSquaresPerAxis, quadrature);
            IReadOnlyList<GaussPoint> gaussPoints = volumeIntegration.GenerateIntegrationPoints(element);

            if (numSquaresPerAxis % 4 == 0)
            {
                double integral = CalcIntegral(gaussPoints, element, func);
                Assert.Equal(expectedIntegral, integral, 4);
            }
            else
            {
                Func<GaussPoint, bool> isPointInValidRegion = p =>
                {
                    return (p.Xi < -0.5) || (p.Xi > -0.5);
                }; //TODO: This should be provided by the function
                CheckIncorrectIntegration(gaussPoints, element, func, expectedIntegral, isPointInValidRegion);
            }
        }

        private static double CalcIntegral(IReadOnlyList<GaussPoint> gaussPoints, 
            IXFiniteElement element, Func<IXFiniteElement, GaussPoint, double> func)
        {
            double integral = 0;
            foreach (GaussPoint gp in gaussPoints)
            {
                Matrix shapeDerivatives = element.StandardInterpolation.EvaluateNaturalGradientsAt(gp);
                var jacobian = new IsoparametricJacobian2D(element.Nodes, shapeDerivatives);
                integral += func(element, gp) * jacobian.DirectDeterminant * gp.Weight;
            }
            return integral;
        }

        private static void CheckIncorrectIntegration(IReadOnlyList<GaussPoint> gaussPoints, IXFiniteElement element, 
            Func<IXFiniteElement, GaussPoint, double> func, double expectedIntegral, Func<GaussPoint, bool> isValidRegion)
        {
            try
            {
                double integral = CalcIntegral(gaussPoints, element, func);
                Assert.NotEqual(expectedIntegral, integral, 4);
            }
            catch (Exception ex)
            {
                bool gaussPointInInvalidRegion = false;
                foreach (GaussPoint point in gaussPoints)
                {
                    if (!isValidRegion(point))
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
