using System;
using System.Collections.Generic;
using System.Text;

//MODIFICATION NEEDED: use constitutive law
namespace MGroup.XFEM.Materials
{
    public class StructuralInterfaceMaterial
    {
        public StructuralInterfaceMaterial(double interfaceConductivity)
        {
            this.InterfaceConductivity = interfaceConductivity;
        }

        public double InterfaceConductivity { get; }

        public StructuralInterfaceMaterial Clone() => new StructuralInterfaceMaterial(this.InterfaceConductivity);
    }
}
