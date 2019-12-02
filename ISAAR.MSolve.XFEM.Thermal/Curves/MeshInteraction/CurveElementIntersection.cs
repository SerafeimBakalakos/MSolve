using System;
using System.Collections.Generic;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Thermal.Entities;

namespace ISAAR.MSolve.XFEM.Thermal.Curves.MeshInteraction
{
    public class CurveElementIntersection
    {
        public CurveElementIntersection(RelativePositionCurveElement relativePosition, NaturalPoint[] intersectionPoints, 
            XNode[] contactNodes)
        {
            this.RelativePosition = relativePosition;

            if (intersectionPoints.Length > 2)
            {
                throw new NotImplementedException("Intersection points must be 0, 1 or 2, but were " + intersectionPoints.Length);
            }
            this.IntersectionPoints = intersectionPoints;
            this.ContactNodes = contactNodes;
        }

        public XNode[] ContactNodes { get; }

        public NaturalPoint[] IntersectionPoints { get; } //TODO: Perhaps these should be empty in the tangent cases

        public RelativePositionCurveElement RelativePosition { get; }


        //TODO: What happens if the interface coincides with the element side? The element side also belongs to another element.
        //      Should I make sure the integral is calculated only once?
        //TODO: Is the orientation of the curve important? I remember that it depends on if we integrate a scalar or vector field.
        public GaussPoint[] GetIntegrationPointsAlongIntersection(int numIntegrationPoints) 
        {
            if (IntersectionPoints.Length < 2) return new GaussPoint[0];

            NaturalPoint start = IntersectionPoints[0];
            NaturalPoint end = IntersectionPoints[1];

            double detJ = 0.5 * start.CalcDistanceFrom(end);
            IReadOnlyList<GaussPoint> gaussPoints1D =
                GaussLegendre1D.GetQuadratureWithOrder(numIntegrationPoints).IntegrationPoints;
            var gaussPoints2D = new GaussPoint[numIntegrationPoints];
            for (int i = 0; i < numIntegrationPoints; ++i)
            {
                GaussPoint gp1D = gaussPoints1D[i];
                double a = 0.5 * (1.0 - gp1D.Xi);
                double b = 0.5 * (1.0 + gp1D.Xi);
                double xi = a * start.Xi + b * end.Xi;
                double eta = a * start.Eta + b * end.Eta;
                double zeta = a * start.Zeta + b * end.Zeta;
                gaussPoints2D[i] = new GaussPoint(xi, eta, zeta, gp1D.Weight * detJ);
            }
            return gaussPoints2D;
        }
    }
}
