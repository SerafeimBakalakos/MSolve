using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.XFEM.Multiphase.Materials
{
    public class ThermalInterfaceMaterial
    {
        public ThermalInterfaceMaterial(double interfaceConductivity, double phaseJumpCoefficient)
        {
            this.InterfaceConductivity = interfaceConductivity;
            this.PhaseJumpCoefficient = phaseJumpCoefficient;
        }

        public double InterfaceConductivity { get; }
        public double PhaseJumpCoefficient { get; }
    }
}
