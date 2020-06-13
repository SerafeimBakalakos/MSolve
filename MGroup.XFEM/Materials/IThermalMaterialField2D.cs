using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Materials
{
    public interface IThermalMaterialField2D
    {
        ThermalInterfaceMaterial FindInterfaceMaterialAt(PhaseBoundary2D phaseBoundary);
        ThermalMaterial FindMaterialAt(IPhase2D phase);
    }
}
