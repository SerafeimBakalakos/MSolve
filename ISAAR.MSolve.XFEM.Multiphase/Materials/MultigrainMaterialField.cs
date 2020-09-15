using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Materials
{
    public class MultigrainMaterialField : IThermalMaterialField
    {
        private readonly ThermalMaterial grainMaterial;
        private readonly ThermalInterfaceMaterial interfaceMaterial;

        public MultigrainMaterialField(double grainConductivity, double interfaceConductivity, double specialHeatCoeff = 1.0)
        {
            this.grainMaterial = new ThermalMaterial(grainConductivity, specialHeatCoeff);
            this.interfaceMaterial = new ThermalInterfaceMaterial(interfaceConductivity);
        }

        public ThermalInterfaceMaterial FindInterfaceMaterialAt(PhaseBoundary phaseBoundary) => interfaceMaterial.Clone();

        public ThermalMaterial FindMaterialAt(IPhase phase) => grainMaterial.Clone();
    }
}
