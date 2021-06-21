using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using System.Diagnostics;

//HERE: order of vertices must be so that the normal of each segment points towards the positive region.
namespace MGroup.XFEM.Geometry.LSM
{
    public interface ILsmElementInteraction
    {
        (RelativePositionCurveElement relativePosition, IntersectionMesh2D intersectionMesh)
            FindIntersection(IList<int> nodeIDs, List<double[]> nodeCoords, List<double> nodeLevelSets);
    }
}
