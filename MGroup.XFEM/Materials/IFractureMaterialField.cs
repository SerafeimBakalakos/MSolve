using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.Materials.Interfaces;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Materials
{
    public interface IFractureMaterialField
    {
        double YoungModulus { get; }
        double EquivalentYoungModulus { get; }
        double PoissonRatio { get; }
        double EquivalentPoissonRatio { get; }

        IContinuumMaterial FindMaterialAt(XPoint point);
    }
}
