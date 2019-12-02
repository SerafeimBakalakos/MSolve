using System.Collections.Generic;
using ISAAR.MSolve.XFEM.Thermal.Entities;
using ISAAR.MSolve.XFEM.Thermal.Curves;

namespace ISAAR.MSolve.XFEM.Thermal.MaterialInterface.SingularityResolving
{
    public interface IHeavisideSingularityResolver
    {
        HashSet<XNode> FindHeavisideNodesToRemove(ICurve2D curve, IEnumerable<XNode> heavisideNodes);
    }
}
