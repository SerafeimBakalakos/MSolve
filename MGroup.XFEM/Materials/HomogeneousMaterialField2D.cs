using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Materials;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Materials
{
    public class HomogeneousMaterialField2D : IStructuralMaterialField
    {
        private readonly ElasticMaterial2D material;

        public HomogeneousMaterialField2D(double youngModulus, double poissonRatio, bool planeStress)
        {
            material = new ElasticMaterial2D(planeStress ? StressState2D.PlaneStress : StressState2D.PlaneStrain);
            material.YoungModulus = youngModulus;
            material.PoissonRatio = poissonRatio;
        }

        public ElasticMaterial2D FindMaterialAt(XPoint point)
            => material;
    }
}
