using System.Collections.Generic;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.XFEM_OLD.Elements;
using ISAAR.MSolve.XFEM_OLD.Entities;

namespace ISAAR.MSolve.XFEM_OLD.CrackGeometry.HeavisideSingularityResolving
{
    public interface IHeavisideSingularityResolver
    {
        ISet<XNode> FindHeavisideNodesToRemove(ISingleCrack crack, IMesh2D<XNode, XContinuumElement2D> mesh, 
            ISet<XNode> heavisideNodes);

        ISet<XNode> FindHeavisideNodesToRemove(ISingleCrack crack, IReadOnlyList<XNode> heavisideNodes,
            IReadOnlyList<ISet<XContinuumElement2D>> nodalSupports);
    }
}
