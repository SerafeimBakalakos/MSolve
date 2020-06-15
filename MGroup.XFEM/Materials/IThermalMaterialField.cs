using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Materials
{
    public interface IThermalMaterialField
    {
        ThermalInterfaceMaterial FindInterfaceMaterialAt(PhaseBoundary phaseBoundary);
        ThermalMaterial FindMaterialAt(IPhase phase);
    }
}
