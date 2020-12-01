using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Materials
{
    public interface IThermalMaterialField
    {
        ThermalInterfaceMaterial FindInterfaceMaterialAt(IPhaseBoundary phaseBoundary);
        ThermalMaterial FindMaterialAt(IPhase phase);
    }
}
