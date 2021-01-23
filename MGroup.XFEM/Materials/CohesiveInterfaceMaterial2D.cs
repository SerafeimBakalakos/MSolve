using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;

//MODIFICATION NEEDED: use constitutive law
namespace MGroup.XFEM.Materials
{
    public class CohesiveInterfaceMaterial2D
    {
        public CohesiveInterfaceMaterial2D(IMatrix interfaceConductivity)
        {
            this.ConstitutiveMatrix = interfaceConductivity;
        }

        /// <summary>
        /// In 2D: [ dtn/d[un], dtn/d[us] ; dts/d[un], dts/d[us] ], where s is the tangential direction to the interface and 
        /// n the normal direction.
        /// </summary>
        public IMatrix ConstitutiveMatrix { get; }

        public CohesiveInterfaceMaterial2D Clone() => new CohesiveInterfaceMaterial2D(ConstitutiveMatrix.Copy());
    }
}
