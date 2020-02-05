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
        }

        public double InterfaceConductivity { get; }
    }
}
