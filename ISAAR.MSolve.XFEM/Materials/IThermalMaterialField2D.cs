using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.XFEM.Elements;

namespace ISAAR.MSolve.XFEM.Materials
{
    public interface IThermalMaterialField2D
    {
        ThermalMaterial GetMaterialAt(IXFiniteElement element, double[] shapeFunctionsAtNaturalPoint);
    }
}
