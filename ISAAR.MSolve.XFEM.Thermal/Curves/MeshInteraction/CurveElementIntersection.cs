using System;
using System.Collections.Generic;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.ThermalOLD.Entities;

//TODO: This is not such a good idea after all, since it is too simple. It would be better to have :
//      1) an interface that defines the intersected segment between a curve and an element. It would be responsible for 
//      generating Gauss points, but it would also be able to handle curved segments, kinks and segments that end inside the 
//      element.
//      2) Identifying the relative position between an element and a curve should probably be done separately from identifying 
//      the intersection (but perhaps they can be called/cached together for performance)
//      3) Corner cases, such as curves tangent to element's edges/nodes should be handled more carefully. A dedicated class 
//      other than the one used for intersections is probably needed.
//      4) Curve tangent to element edge does provide an intersection segment, but it is a) easier to calculate b) shared among 
//      elements. Thus the intersection segments should store which elements they refer to and divide the Gauss point weights 
//      over the multiplicity
//      5) Taking into account both the natural and cartesian system for all data is needed. For efficiency only 1 should be 
//      used at first and the other should be computed only when needed  
namespace ISAAR.MSolve.XFEM.ThermalOLD.Curves.MeshInteraction
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
