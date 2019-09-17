using ISAAR.MSolve.Materials;
using ISAAR.MSolve.Materials.Interfaces;
using ISAAR.MSolve.XFEM.Materials;
using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.XFEM.Transfer.Materials
{
    [Serializable]
    public class HomogeneousElasticMaterial2DDto : IXMaterialFieldDto
    {
        public int id;
        public bool planeStress;
        public double poissonRatio;
        public double thickness;
        public double youngModulus;

        public HomogeneousElasticMaterial2DDto(HomogeneousElasticMaterial2D material)
        {
            this.id = material.ID;
            this.thickness = material.HomogeneousThickness;
            this.planeStress = material.HomogeneousYoungModulus == material.HomogeneousEquivalentYoungModulus;
            this.youngModulus = material.HomogeneousYoungModulus;
            this.poissonRatio = material.HomogeneousPoissonRatio;
        }

        public int ID => id;

        public IXMaterialField2D Deserialize()
        {
            if (planeStress)
            {
                return HomogeneousElasticMaterial2D.CreateMaterialForPlaneStress(id, youngModulus, poissonRatio, thickness);
            }
            else return HomogeneousElasticMaterial2D.CreateMaterialForPlaneStrain(id, youngModulus, poissonRatio);
        }
    }
}
