using System.Collections.Generic;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.XFEM.Thermal.Entities;

namespace ISAAR.MSolve.XFEM.Thermal.MaterialInterface.SingularityResolving
{
    public interface IHeavisideSingularityResolver
    {
        HashSet<XNode> FindHeavisideNodesToRemove(IEnumerable<XNode> heavisideNodes);
    }
}
