using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.XFEM.Thermal.Elements;
using ISAAR.MSolve.XFEM.Thermal.Enrichments.Functions;
using ISAAR.MSolve.XFEM.Thermal.Entities;
using ISAAR.MSolve.XFEM.Thermal.MaterialInterface.Geometry;

namespace ISAAR.MSolve.XFEM.Thermal.Enrichments.Items
{
    public class ThermalInterfaceEnrichment
    {
        //public enum Subdomain { Positive, Negative, Boundary }

        private readonly HalfSignFunction enrichmentFunction;
        private HashSet<XThermalElement2D> affectedElements;

        public ThermalInterfaceEnrichment(IMaterialInterfaceGeometry discontinuity, double interfaceResistance)
        {
            this.Discontinuity = discontinuity;
            this.InterfaceResistance = interfaceResistance;
            this.enrichmentFunction = new HalfSignFunction();
            this.Dofs = new EnrichedDof[] { new EnrichedDof(enrichmentFunction, ThermalDof.Temperature) };
            this.affectedElements = new HashSet<XThermalElement2D>();
        }

        public IMaterialInterfaceGeometry Discontinuity { get; }

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

        public EvaluatedFunction[] EvaluateAllAt(IXFiniteElement element,
             double[] shapeFunctionsAtNaturalPoint)
        {
            double signedDistance = Discontinuity.SignedDistanceOf(element, shapeFunctionsAtNaturalPoint);
            return new EvaluatedFunction[] { enrichmentFunction.EvaluateAllAt(signedDistance) };
        }

        public IReadOnlyList<CartesianPoint> IntersectionPointsForIntegration(IXFiniteElement element)
            => throw new NotImplementedException();

    }
}
