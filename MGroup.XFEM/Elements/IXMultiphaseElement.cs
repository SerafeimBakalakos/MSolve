using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Integration;

namespace MGroup.XFEM.Elements
{
    public interface IXMultiphaseElement : IXFiniteElement
    {
        IEnumerable<GaussPoint> BulkIntegrationPoints { get; } //TODO: This should probably be in IXFiniteElement

        IEnumerable<GaussPoint> BoundaryIntegrationPoints { get; } //TODO: This should probably be in IXFiniteElement

        HashSet<IPhase> Phases { get; }

        Dictionary<IPhaseBoundary, IElementDiscontinuityInteraction> PhaseIntersections { get; }
    }
}
