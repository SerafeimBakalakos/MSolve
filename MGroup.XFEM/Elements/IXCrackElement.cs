using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.XFEM.Elements
{
    public interface IXCrackElement : IXFiniteElement
    {
        bool IsIntersectedElement { get; set; } //TODO: Should this be in IXFiniteElement? It is similar to storing the triangulation mesh.

        bool IsTipElement { get; set; }
    }
}
