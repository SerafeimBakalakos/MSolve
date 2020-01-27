using ISAAR.MSolve.Materials;
using ISAAR.MSolve.XFEM.ThermalOLD.Elements;

namespace ISAAR.MSolve.XFEM.ThermalOLD.Materials
{
    public interface IThermalMaterialField2D
    {
        ThermalMaterial GetMaterialAt(IXFiniteElement element, double[] shapeFunctionsAtNaturalPoint);
    }
}
