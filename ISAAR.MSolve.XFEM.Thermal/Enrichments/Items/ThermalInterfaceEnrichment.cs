﻿using System;
using System.Collections.Generic;
using System.Linq;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.ThermalOLD.Elements;
using ISAAR.MSolve.XFEM.ThermalOLD.Enrichments.Functions;
using ISAAR.MSolve.XFEM.ThermalOLD.Entities;
using ISAAR.MSolve.XFEM.ThermalOLD.Curves;

namespace ISAAR.MSolve.XFEM.ThermalOLD.Enrichments.Items
{
    public class ThermalInterfaceEnrichment : IEnrichmentItem
    {
        private readonly HalfSignFunction2D enrichmentFunction;
        private HashSet<XThermalElement2D> affectedElements;

        public ThermalInterfaceEnrichment(ICurve2D discontinuity, double interfaceResistance)
        {
            this.Discontinuity = discontinuity;
            this.InterfaceResistance = interfaceResistance;
            this.enrichmentFunction = new HalfSignFunction2D();
            this.Dofs = new EnrichedDof[] { new EnrichedDof(enrichmentFunction, ThermalDof.Temperature) };
            this.affectedElements = new HashSet<XThermalElement2D>();
        }

        public ICurve2D Discontinuity { get; }

        public IReadOnlyList<EnrichedDof> Dofs { get; }

        public double InterfaceResistance { get; } //TODO: This should be accessed from some material class.

        public void EnrichElement(XThermalElement2D element)
        {
            if (!affectedElements.Contains(element))
            {
                affectedElements.Add(element);
                element.EnrichmentItems.Add(this);
            }
        }

        public EvaluatedFunction[] EvaluateAllAt(IXFiniteElement element, double[] shapeFunctionsAtNaturalPoint)
        {
            double signedDistance = Discontinuity.SignedDistanceOf(element, shapeFunctionsAtNaturalPoint);
            return new EvaluatedFunction[] { enrichmentFunction.EvaluateAllAt(signedDistance) };
        }

        public double[] EvaluateFunctionsAt(XNode node)
        {
            double signedDistance = Discontinuity.SignedDistanceOf(node);
            return new double[] { enrichmentFunction.EvaluateAt(signedDistance) };
        }

        public IList<EvaluatedFunction[]> EvaluateAllAtSubtriangleVertices(IXFiniteElement element,
            IList<double[]> shapeFunctionsAtVertices, double[] shapeFunctionsAtCentroid)
        {
            int numVertices = shapeFunctionsAtVertices.Count;
            var signedDistancesAtVertices = new double[numVertices];
            for (int v = 0; v < numVertices; ++v)
            {
                double[] N = shapeFunctionsAtVertices[v];
                signedDistancesAtVertices[v] = Discontinuity.SignedDistanceOf(element, N);
            }
            double signedDistanceAtCentroid = Discontinuity.SignedDistanceOf(element, shapeFunctionsAtCentroid);

            EvaluatedFunction[] enrichmentFunctions =
                enrichmentFunction.EvaluateAllAtSubtriangleVertices(signedDistancesAtVertices, signedDistanceAtCentroid);
            return enrichmentFunctions.Select(e => new EvaluatedFunction[] { e }).ToList();
        }

        public IList<double[]> EvaluateFunctionsAtSubtriangleVertices(IXFiniteElement element, 
            IList<double[]> shapeFunctionsAtVertices, double[] shapeFunctionsAtCentroid)
        {
            int numVertices = shapeFunctionsAtVertices.Count;
            var signedDistancesAtVertices = new double[numVertices];
            for (int v = 0; v < numVertices; ++v)
            {
                double[] N = shapeFunctionsAtVertices[v];
                signedDistancesAtVertices[v] = Discontinuity.SignedDistanceOf(element, N);
            }
            double signedDistanceAtCentroid = Discontinuity.SignedDistanceOf(element, shapeFunctionsAtCentroid);

            double[] enrichmentFunctions = 
                enrichmentFunction.EvaluateAtSubtriangleVertices(signedDistancesAtVertices, signedDistanceAtCentroid);
            return enrichmentFunctions.Select(e => new double[] { e }).ToList();
        }
    }
}