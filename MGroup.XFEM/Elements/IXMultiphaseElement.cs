using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Phases;

namespace MGroup.XFEM.Elements
{
    public interface IXMultiphaseElement : IXFiniteElement
    {
        IReadOnlyList<GaussPoint> BulkIntegrationPoints { get; } //TODO: This should probably be in IXFiniteElement

        //TODO: This should probably be in IXFiniteElement.
        //TODO: This is only used for cohesive interfaces. Otherwise their computation is avoided. Not the best design.
        IReadOnlyList<GaussPoint> BoundaryIntegrationPoints { get; }

        //TODO: This should probably be in IXFiniteElement.
        //TODO: This is only used for cohesive interfaces. Otherwise their computation is avoided. Not the best design.
        IReadOnlyList<double[]> BoundaryIntegrationPointNormals { get; }


        HashSet<IPhase> Phases { get; }

        //TODO: If the interfaces are not cohesive, this is not used in the XFEM analysis. Integration classes use
        //      Dictionary<int, IElementDiscontinuityInteraction> IXFiniteElement.InteractingDiscontinuities. Therefore, there is
        //      no reason to compute and store this for the XFEM analysis. Perhaps we do need it for the geometric classes though.
        Dictionary<IPhaseBoundary, IElementDiscontinuityInteraction> PhaseIntersections { get; }
    }
}
