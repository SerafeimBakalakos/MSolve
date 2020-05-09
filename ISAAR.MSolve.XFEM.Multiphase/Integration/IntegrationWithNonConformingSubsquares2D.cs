using System.Collections.Generic;
using System.Diagnostics;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;

//TODO: How do I identify standard/enriched elements? Using the phases or the nodal enrichments?
namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Integration
{
    /// <summary>
    /// TODO: This rule is actually independent from the element and its elements can be cached, albeit not in a 
    /// static manner. Should I put it with the standard quadratures?
    /// TODO: Ensure this is not used for anything other than Quadrilaterals.
    /// </summary>
    public class IntegrationWithNonConformingSubsquares2D : IIntegrationStrategy
    {
        private readonly GaussLegendre2D quadratureInSubcells;
        private readonly IQuadrature2D standardQuadrature;

        public IntegrationWithNonConformingSubsquares2D(IQuadrature2D standardQuadrature) : 
            this(standardQuadrature, 4, GaussLegendre2D.GetQuadratureWithOrder(2,2))
        {
        }

        public IntegrationWithNonConformingSubsquares2D(IQuadrature2D standardQuadrature, 
            int subcellsPerAxis, GaussLegendre2D quadratureInSubcells)
        {
            this.standardQuadrature = standardQuadrature;
            this.SubcellsPerAxis = subcellsPerAxis;
            this.quadratureInSubcells = quadratureInSubcells;
        }
        public int SubcellsPerAxis { get; }

        public IReadOnlyList<GaussPoint> GenerateIntegrationPoints(IXFiniteElement element)
        {
            // Standard elements
            if (UseStandardQuadrature(element)) return standardQuadrature.IntegrationPoints;

            // Enriched elements
            var points = new List<GaussPoint>();
            double length = 2.0 / SubcellsPerAxis;
            for (int i = 0; i < SubcellsPerAxis; ++i)
            {
                for (int j = 0; j < SubcellsPerAxis; ++j)
                {
                    // The borders of the subrectangle
                    double xiMin = -1.0 + length * i;
                    double xiMax = -1.0 + length * (i+1);
                    double etaMin = -1.0 + length * j;
                    double etaMax = -1.0 + length * (j + 1);

                    foreach(var subgridPoint in quadratureInSubcells.IntegrationPoints)
                    {
                        // Transformation from the system of the subrectangle to the natural system of the element
                        double naturalXi = subgridPoint.Xi * (xiMax - xiMin) / 2.0 + (xiMin + xiMax) / 2.0;
                        double naturalEta = subgridPoint.Eta * (etaMax - etaMin) / 2.0 + (etaMin + etaMax) / 2.0;
                        double naturalWeight = subgridPoint.Weight * (xiMax - xiMin) / 2.0 * (etaMax - etaMin) / 2.0;
                        points.Add(new GaussPoint(naturalXi, naturalEta, naturalWeight));
                    }
                }
            }
            return points;
        }

        private bool UseStandardQuadrature(IXFiniteElement element)
        {
            Debug.Assert(element.Phases.Count > 0);
            if (element.Phases.Count == 1) return true;
            else return false;
        }
    }
}
