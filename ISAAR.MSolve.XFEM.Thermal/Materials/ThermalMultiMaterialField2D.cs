using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.XFEM.Thermal.Elements;
using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod;

namespace ISAAR.MSolve.XFEM.Thermal.Materials
{
    public class ThermalMultiMaterialField2D : IThermalMaterialField2D
    {
        private readonly Dictionary<int, ThermalMaterial> materials;
        private readonly ILsmCurve2D materialInterface;

        public ThermalMultiMaterialField2D(ThermalMaterial materialPositive, ThermalMaterial materialNegative,
            ILsmCurve2D materialInterface)
        {
            this.materials = new Dictionary<int, ThermalMaterial>();
            this.materials[0] = materialPositive;
            this.materials[1] = materialNegative;
            this.materialInterface = materialInterface;
        }

        //TODO: If we use narrow band level set then foreach interface, then this method will not work for the standard/blending
        //      elements outside the narrow band. 
        public ThermalMaterial GetMaterialAt(IXFiniteElement element, double[] shapeFunctionsAtNaturalPoint)
        {
            double levelSet = materialInterface.SignedDistanceOf(element, shapeFunctionsAtNaturalPoint);
            if (levelSet >= 0.0) return materials[0];
            else return materials[1];
        }
    }
}
