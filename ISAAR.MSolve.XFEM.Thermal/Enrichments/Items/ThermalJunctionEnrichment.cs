using System;
using System.Collections.Generic;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Thermal.Elements;
using ISAAR.MSolve.XFEM.Thermal.Enrichments.Functions;
using ISAAR.MSolve.XFEM.Thermal.Entities;
using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod;

namespace ISAAR.MSolve.XFEM.Thermal.Enrichments.Items
{
    public class ThermalJunctionEnrichment : IEnrichmentItem
    {
        private readonly JunctionFunction2D enrichmentFunction;
        //private readonly ILsmCurve2D[] curves;
        //private readonly double[] interfaceResistances;
        private readonly MaterialPhase[] phases;

        public ThermalJunctionEnrichment(MaterialPhase[] phases /*ILsmCurve2D[] curves, double[] interfaceResistances*/)
        {
            this.phases = phases;
            //this.curves = curves;
            //this.interfaceResistances = interfaceResistances;
            this.enrichmentFunction = new JunctionFunction2D(phases.Length);
            this.Dofs = new EnrichedDof[] { new EnrichedDof(enrichmentFunction, ThermalDof.Temperature) };
        }

        public IReadOnlyList<EnrichedDof> Dofs { get; }
        public object Discontinuity { get; private set; }

        public EvaluatedFunction[] EvaluateAllAt(IXFiniteElement element, double[] shapeFunctionsAtNaturalPoint)
        {
            foreach (MaterialPhase phase in phases)
            {
                bool isInside = phase.IsInside(element, shapeFunctionsAtNaturalPoint);
                if (isInside) return new EvaluatedFunction[] { enrichmentFunction.EvaluateAllAt(phase.ID) };
            }
            throw new ArgumentException("This node does not belong to any of the phases in this junction");
        }

        public IList<EvaluatedFunction[]> EvaluateAllAtSubtriangleVertices(IXFiniteElement element, IList<double[]> shapeFunctionsAtVertices, double[] shapeFunctionsAtCentroid)
        {
            throw new NotImplementedException();
        }

        public double[] EvaluateFunctionsAt(XNode node)
        {
            foreach (MaterialPhase phase in phases)
            {
                bool isInside = phase.IsInside(node);
                if (isInside) return new double[] { enrichmentFunction.EvaluateAt(phase.ID) };
            }
            throw new ArgumentException("This node does not belong to any of the phases in this junction");
        }

        public IList<double[]> EvaluateFunctionsAtSubtriangleVertices(IXFiniteElement element, IList<double[]> shapeFunctionsAtVertices, double[] shapeFunctionsAtCentroid)
        {
            throw new NotImplementedException();
        }
    }
}
