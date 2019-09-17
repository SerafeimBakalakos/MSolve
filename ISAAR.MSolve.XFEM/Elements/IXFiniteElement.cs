using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.XFEM.Entities;
using ISAAR.MSolve.XFEM.Materials;

namespace ISAAR.MSolve.XFEM.Elements
{
    public interface IXFiniteElement : IElement, IElementType, ICell<XNode>
    {
        IXMaterialField2D Material { get; }

        IReadOnlyList<XNode> Nodes { get; }
        XSubdomain Subdomain { get; set; }
    }
}