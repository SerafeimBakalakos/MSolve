using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;

namespace ISAAR.MSolve.XFEM.Multiphase.Integration
{
    /// <summary>
    /// Assumes that finite elements are 1st order (Tri3 or Quad4) and that the boundary segment is linear.
    /// </summary>
    public class LinearBoundaryIntegration : IBoundaryIntegration
    {
        private readonly GaussLegendre1D quadrature1D;
        public LinearBoundaryIntegration(GaussLegendre1D quadrature1D)
        {
            this.quadrature1D = quadrature1D;
        }

        ////TODO: What happens if the interface coincides with the element side? The element side also belongs to another element.
        ////      Should I make sure the integral is calculated only once?
        public IReadOnlyList<GaussPoint> GenerateIntegrationPoints(IXFiniteElement element, 
            CurveElementIntersection intersection)
        {
            Debug.Assert(intersection.RelativePosition != RelativePositionCurveElement.Disjoint);
            Debug.Assert(intersection.IntersectionPoints.Length == 2);
            if (intersection.RelativePosition == RelativePositionCurveElement.Tangent)
            {
                throw new NotImplementedException("I would need to take half the total integral from both elements");
            }

            NaturalPoint naturalA = intersection.IntersectionPoints[0];
            NaturalPoint naturalB = intersection.IntersectionPoints[1];

            // Absolute determinant of Jacobian of mapping from auxiliary to cartesian system. Constant for all Gauss points.
            CartesianPoint cartesianA = element.StandardInterpolation.TransformNaturalToCartesian(element.Nodes, naturalA);
            CartesianPoint cartesianB = element.StandardInterpolation.TransformNaturalToCartesian(element.Nodes, naturalB);
            double detJ = Math.Abs(0.5 * cartesianA.CalculateDistanceFrom(cartesianB));

            int numIntegrationPoints = quadrature1D.IntegrationPoints.Count;
            var integrationPoints = new GaussPoint[numIntegrationPoints];
            for (int i = 0; i < numIntegrationPoints; ++i)
            {
                GaussPoint gp1D = quadrature1D.IntegrationPoints[i];
                double a = 0.5 * (1.0 - gp1D.Xi);
                double b = 0.5 * (1.0 + gp1D.Xi);
                double xi = a * naturalA.Xi + b * naturalB.Xi;
                double eta = a * naturalA.Eta + b * naturalB.Eta;
                integrationPoints[i] = new GaussPoint(xi, eta, gp1D.Weight * detJ);
            }

            return integrationPoints;
        }
    }
}
