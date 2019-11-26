using System.Collections.Generic;
using ISAAR.MSolve.XFEM.Thermal.Entities;
using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod;

namespace ISAAR.MSolve.XFEM.Thermal.MaterialInterface.SingularityResolving
{
    public interface IHeavisideSingularityResolver
    {
        HashSet<XNode> FindHeavisideNodesToRemove(ILsmCurve2D curve, IEnumerable<XNode> heavisideNodes);
    }
}
