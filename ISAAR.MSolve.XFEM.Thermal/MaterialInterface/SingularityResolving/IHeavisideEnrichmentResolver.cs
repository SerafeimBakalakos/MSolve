using System.Collections.Generic;
using ISAAR.MSolve.XFEM.ThermalOLD.Entities;
using ISAAR.MSolve.XFEM.ThermalOLD.Curves;

namespace ISAAR.MSolve.XFEM.ThermalOLD.MaterialInterface.SingularityResolving
{
    public interface IHeavisideSingularityResolver
    {
        HashSet<XNode> FindHeavisideNodesToRemove(ICurve2D curve, IEnumerable<XNode> heavisideNodes);
    }
}
