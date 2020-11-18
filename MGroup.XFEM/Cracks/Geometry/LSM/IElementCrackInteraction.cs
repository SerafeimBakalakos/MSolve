using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Integration;

//TODO: Merge with the general IElementGeometryIntersection
namespace MGroup.XFEM.Cracks.Geometry.LSM
{
    public interface IElementCrackInteraction : IElementDiscontinuityInteraction
    {
        bool TipInteractsWithElement { get; }
    }
}
