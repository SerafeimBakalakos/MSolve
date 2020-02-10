using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM.Multiphase.Materials
{
    public interface IThermalMaterialField
    {
        ThermalInterfaceMaterial FindInterfaceMaterialAt(PhaseBoundary phaseBoundary);
        ThermalMaterial FindMaterialAt(IPhase phase);
    }
}
