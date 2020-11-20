using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.XFEM.Geometry
{
    public interface IElementOpenGeometryInteraction : IElementDiscontinuityInteraction
    {
        bool TipInteractsWithElement { get; }
    }
}
