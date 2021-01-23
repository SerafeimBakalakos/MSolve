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

        IReadOnlyList<GaussPoint> BoundaryIntegrationPoints { get; } //TODO: This should probably be in IXFiniteElement

        IReadOnlyList<double[]> BoundaryIntegrationPointNormals { get; } //TODO: This should probably be in IXFiniteElement


        HashSet<IPhase> Phases { get; }

        Dictionary<IPhaseBoundary, IElementDiscontinuityInteraction> PhaseIntersections { get; }
    }
}
