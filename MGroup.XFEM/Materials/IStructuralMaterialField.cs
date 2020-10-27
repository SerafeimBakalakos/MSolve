using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Materials;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Materials
{
    public interface IStructuralMaterialField
    {
        ElasticMaterial2D FindMaterialAt(XPoint point);
    }
}
