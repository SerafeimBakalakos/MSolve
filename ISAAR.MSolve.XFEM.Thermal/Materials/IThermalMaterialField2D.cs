using ISAAR.MSolve.Materials;
using ISAAR.MSolve.XFEM_OLD.Thermal.Elements;

namespace ISAAR.MSolve.XFEM_OLD.Thermal.Materials
{
    public interface IThermalMaterialField2D
    {
        ThermalMaterial GetMaterialAt(IXFiniteElement element, double[] shapeFunctionsAtNaturalPoint);
    }
}
