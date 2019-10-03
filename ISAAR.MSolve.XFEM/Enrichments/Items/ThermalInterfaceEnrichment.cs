using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.XFEM.CrackGeometry.MaterialInterfaces;
using ISAAR.MSolve.XFEM.Elements;
using ISAAR.MSolve.XFEM.Enrichments.Functions;
using ISAAR.MSolve.XFEM.Entities;
using ISAAR.MSolve.XFEM.FreedomDegrees;
using ISAAR.MSolve.XFEM.Utilities;

namespace ISAAR.MSolve.XFEM.Enrichments.Items
{
    public class ThermalInterfaceEnrichment
    {
        //public enum Subdomain { Positive, Negative, Boundary }

        private readonly HalfSignFunction enrichmentFunction;
        private HashSet<XThermalElement2D> affectedElements;

        public ThermalInterfaceEnrichment(IMaterialInterface discontinuity, double interfaceResistance)
        {
            this.Discontinuity = discontinuity;
            this.InterfaceResistance = interfaceResistance;
            this.enrichmentFunction = new HalfSignFunction();
            this.Dofs = new EnrichedDof[] { new EnrichedDof(enrichmentFunction, ThermalDof.Temperature) };
            this.affectedElements = new HashSet<XThermalElement2D>();
        }

        public IMaterialInterface Discontinuity { get; }

        public IReadOnlyList<EnrichedDof> Dofs { get; protected set; }

        public double InterfaceResistance { get; } //TODO: This should be accessed from some material class.

        public void EnrichElement(XThermalElement2D element)
        {
            if (!affectedElements.Contains(element))
            {
                affectedElements.Add(element);
                element.EnrichmentItems.Add(this);
            }
        }

        public double[] EvaluateFunctionsAt(XNode node)
        {
            double signedDistance = Discontinuity.SignedDistanceOf(node);
            return new double[] { enrichmentFunction.EvaluateAt(signedDistance) };
        }

        public EvaluatedFunction2D[] EvaluateAllAt(IXFiniteElement element,
             double[] shapeFunctionsAtNaturalPoint)
        {
            double signedDistance = Discontinuity.SignedDistanceOf(element, shapeFunctionsAtNaturalPoint);
            return new EvaluatedFunction2D[] { enrichmentFunction.EvaluateAllAt(signedDistance) };
        }

        //// TODO: add some tolerance when checking around 0. Perhaps all this is not needed though and I could even 
        //// ignore points on the interface. It certainly needs a better name
        ///// <summary>
        ///// Finds the subdomain where the requested cartesian point lies.
        ///// </summary>
        ///// <param name="point"></param>
        ///// <param name="subdomain">The posi</param>
        ///// <returns></returns>
        //public Subdomain LocatePoint(CartesianPoint point)
        //{
        //    int sign = Math.Sign(Discontinuity.SignedDistanceOf(point));
        //    if (sign < 0) return Subdomain.Negative;
        //    else if (sign > 0) return Subdomain.Positive;
        //    else return Subdomain.Boundary;
        //}

        public IReadOnlyList<CartesianPoint> IntersectionPointsForIntegration(XContinuumElement2D element)
            => throw new NotImplementedException();

    }
}
