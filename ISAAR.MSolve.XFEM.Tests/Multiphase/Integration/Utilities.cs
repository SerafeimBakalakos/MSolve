using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.FEM.Interpolation.Jacobians;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.XFEM.Multiphase.Elements;

namespace ISAAR.MSolve.XFEM.Tests.Multiphase.Integration
{
    internal static class Utilities
    {
        internal static double CalcIntegral(IReadOnlyList<GaussPoint> gaussPoints,
            BenchmarkDomain domain, IBenchmarkVolumeFunction func)
        {
            IXFiniteElement element = domain.Element;
            double integral = 0;
            foreach (GaussPoint gp in gaussPoints)
            {
                double detJ = domain.CalcJacobianDeterminant(gp);
                integral += func.Evaluate(gp, element) * detJ * gp.Weight;
            }
            return integral;
        }

        internal static double CalcPolygonArea(IReadOnlyList<CartesianPoint> points)
        {
            double sum = 0.0;
            for (int i = 0; i < points.Count; ++i)
            {
                CartesianPoint point1 = points[i];
                CartesianPoint point2 = points[(i + 1) % points.Count];
                sum += point1.X * point2.Y - point2.X * point1.Y;
            }
            return Math.Abs(0.5 * sum); // area would be negative if vertices were in counter-clockwise order
        }

        internal static CartesianPoint FindMiddle(CartesianPoint point1, CartesianPoint point2)
        {
            return new CartesianPoint(0.5 * (point1.X + point2.X), 0.5 * (point1.Y + point2.Y));
        }
    }
}
