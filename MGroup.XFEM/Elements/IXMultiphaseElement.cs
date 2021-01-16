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
        //MODIFICATION NEEDED: This should be a list
        IEnumerable<GaussPoint> BulkIntegrationPoints { get; } //TODO: This should probably be in IXFiniteElement

        //MODIFICATION NEEDED: This should be a list
        IEnumerable<GaussPoint> BoundaryIntegrationPoints { get; } //TODO: This should probably be in IXFiniteElement

        HashSet<IPhase> Phases { get; }

        Dictionary<IPhaseBoundary, IElementDiscontinuityInteraction> PhaseIntersections { get; }
    }
}
