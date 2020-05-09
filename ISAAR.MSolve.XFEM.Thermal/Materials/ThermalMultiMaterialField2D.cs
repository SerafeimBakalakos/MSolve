using ISAAR.MSolve.Materials;
using ISAAR.MSolve.XFEM_OLD.Thermal.Elements;
using ISAAR.MSolve.XFEM_OLD.Thermal.Entities;
using ISAAR.MSolve.XFEM_OLD.Thermal.Curves.LevelSetMethod;

namespace ISAAR.MSolve.XFEM_OLD.Thermal.Materials
{
    public class ThermalMultiMaterialField2D : IThermalMaterialField2D
    {
        private readonly ThermalMaterial materialPositive;
        private readonly ThermalMaterial materialNegative;
        private readonly GeometricModel2D geometricModel;

        public ThermalMultiMaterialField2D(ThermalMaterial materialPositive, ThermalMaterial materialNegative,
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
            double min = double.MaxValue;
            foreach (SimpleLsmClosedCurve2D curve in geometricModel.SingleCurves)
            {
                double signedDistance = curve.SignedDistanceOf(element, shapeFunctionsAtNaturalPoint);
                if (signedDistance < min) min = signedDistance;
            }
            if (min >= 0) return materialPositive;
            else return materialNegative;
        }
    }
}
