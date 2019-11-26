using ISAAR.MSolve.Materials;
using ISAAR.MSolve.XFEM.Thermal.Elements;

namespace ISAAR.MSolve.XFEM.Thermal.Materials
{
    public interface IThermalMaterialField2D
    {
        ThermalMaterial GetMaterialAt(IXFiniteElement element, double[] shapeFunctionsAtNaturalPoint);
    }
}
