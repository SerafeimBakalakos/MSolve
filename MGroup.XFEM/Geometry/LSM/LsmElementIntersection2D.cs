using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Elements;

namespace MGroup.XFEM.Geometry.LSM
{
    public class LsmElementIntersection2D : IElementCurveIntersection2D
    {
        private readonly NaturalPoint start;
        private readonly NaturalPoint end;

        public LsmElementIntersection2D(RelativePositionCurveElement relativePosition, IXFiniteElement2D element,
            NaturalPoint start, NaturalPoint end)
        {
            if (relativePosition == RelativePositionCurveElement.Disjoint)
            {
                throw new ArgumentException("There is no intersection between the curve and element");
            }
            this.RelativePosition = relativePosition;
            this.Element = element;
            this.start = start;
            this.end = end;
        }

        public RelativePositionCurveElement RelativePosition { get; }

        public IXFiniteElement2D Element { get; } //TODO: Perhaps this should be defined in the interface

        public List<double[]> ApproximateGlobalCartesian()
        {
            var points = new List<double[]>(2);
            points.Add(Element.Interpolation.TransformNaturalToCartesian(Element.Nodes, start).Coordinates);
            points.Add(Element.Interpolation.TransformNaturalToCartesian(Element.Nodes, end).Coordinates);
            return points;
        }

        //TODO: Perhaps a dedicated IBoundaryIntegration component is needed
        public IReadOnlyList<GaussPoint> GetIntegrationPoints(int order)
        {
            // Conforming curves intersect 2 elements, thus the integral will be computed twice. Halve the weights to avoid 
            // obtaining double the value of the integral.
            double weightModifier = 1.0;
            if (RelativePosition == RelativePositionCurveElement.Conforming) weightModifier = 0.5;

            // Absolute determinant of Jacobian of mapping from auxiliary to cartesian system. Constant for all Gauss points.
            CartesianPoint startCartesian = Element.Interpolation.TransformNaturalToCartesian(Element.Nodes, start);
            CartesianPoint endCartesian = Element.Interpolation.TransformNaturalToCartesian(Element.Nodes, end);
            double detJ = Math.Abs(0.5 * startCartesian.CalculateDistanceFrom(endCartesian));

            var quadrature1D = GaussLegendre1D.GetQuadratureWithOrder(order);
            int numIntegrationPoints = quadrature1D.IntegrationPoints.Count;
            var integrationPoints = new GaussPoint[numIntegrationPoints];
            for (int i = 0; i < numIntegrationPoints; ++i)
            {
                GaussPoint gp1D = quadrature1D.IntegrationPoints[i];
                double N0 = 0.5 * (1.0 - gp1D.Xi);
                double N1 = 0.5 * (1.0 + gp1D.Xi);
                double xi = N0 * start.Xi + N1 * end.Xi;
                double eta = N0 * start.Eta + N1 * end.Eta;
                integrationPoints[i] = new GaussPoint(xi, eta, gp1D.Weight * detJ * weightModifier);
            }

            return integrationPoints;
        }

        public NaturalPoint[] GetPointsForTriangulation()
        {
            return new NaturalPoint[] { start, end };
        }
    }
}
