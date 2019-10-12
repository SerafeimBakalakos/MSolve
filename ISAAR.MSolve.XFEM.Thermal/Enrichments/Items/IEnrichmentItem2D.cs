using System.Collections.Generic;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Thermal.Elements;
using ISAAR.MSolve.XFEM.Thermal.Entities;

namespace ISAAR.MSolve.XFEM.Thermal.Enrichments.Items
{
    // Connects the geometry, model and enrichment function entities.
    // TODO: At this point it does most of the work in 1 class. Appropriate decomposition is needed.
    public interface IEnrichmentItem
    {
        // Perhaps the nodal dof types should be decided by the element type (structural, continuum) in combination with the EnrichmentItem2D and drawn from XContinuumElement2D
        /// <summary>
        /// The order is enrichment function major, axis minor. 
        /// </summary>
        IReadOnlyList<EnrichedDof> Dofs { get; } 

        //IReadOnlyList<XContinuumElement2D> AffectedElements { get; }

        ///// <summary>
        ///// Assigns enrichment functions and their nodal values to each enriched node.
        ///// </summary>
        //void EnrichNodes();

        //void EnrichElement(XContinuumElement2D element);

        IReadOnlyList<CartesianPoint> IntersectionPointsForIntegration(IXFiniteElement element);

        double[] EvaluateFunctionsAt(XNode node);

        EvaluatedFunction[] EvaluateAllAt(NaturalPoint point, IXFiniteElement element, EvalInterpolation2D interpolation);
    }
}
