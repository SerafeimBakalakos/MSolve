using System.Collections.Generic;
using ISAAR.MSolve.XFEM_OLD.Thermal.Entities;
using ISAAR.MSolve.XFEM_OLD.Thermal.Curves;

namespace ISAAR.MSolve.XFEM_OLD.Thermal.MaterialInterface.SingularityResolving
{
    public interface IHeavisideSingularityResolver
    {
        HashSet<XNode> FindHeavisideNodesToRemove(ICurve2D curve, IEnumerable<XNode> heavisideNodes);
    }
}
