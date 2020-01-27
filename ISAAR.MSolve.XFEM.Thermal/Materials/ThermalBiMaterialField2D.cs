using ISAAR.MSolve.Materials;
using ISAAR.MSolve.XFEM.ThermalOLD.Elements;
using ISAAR.MSolve.XFEM.ThermalOLD.Entities;

namespace ISAAR.MSolve.XFEM.ThermalOLD.Materials
{
    public class ThermalBiMaterialField2D : IThermalMaterialField2D
    {
        private readonly ThermalMaterial materialPositive;
        private readonly ThermalMaterial materialNegative;
        private readonly GeometricModel2D geometricModel;

        public ThermalBiMaterialField2D(ThermalMaterial materialPositive, ThermalMaterial materialNegative,
            GeometricModel2D geometricModel)
        {
            this.materialPositive = materialPositive;
            this.materialNegative = materialNegative;
            this.geometricModel = geometricModel;
        }

        //TODO: If we use narrow band level set then foreach interface, then this method will not work for the standard/blending
        //      elements outside the narrow band. 
        public ThermalMaterial GetMaterialAt(IXFiniteElement element, double[] shapeFunctionsAtNaturalPoint)
        {
            double levelSet = geometricModel.SingleCurves[0].SignedDistanceOf(element, shapeFunctionsAtNaturalPoint);
            if (levelSet >= 0.0) return materialPositive;
            else return materialNegative;
        }
    }
}
