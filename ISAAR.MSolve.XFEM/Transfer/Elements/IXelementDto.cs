using ISAAR.MSolve.XFEM.Elements;
using ISAAR.MSolve.XFEM.Entities;
using ISAAR.MSolve.XFEM.Materials;
using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.XFEM.Transfer.Elements
{
    public interface IXElementDto
    {
        IXFiniteElement Deserialize(IReadOnlyDictionary<int, XNode> allNodes, Dictionary<int, IXMaterialField2D> allMaterials);
    }
}
