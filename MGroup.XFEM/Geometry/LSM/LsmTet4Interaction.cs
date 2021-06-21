using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Integration.Quadratures;
using ISAAR.MSolve.Discretization.Mesh;
using System.Diagnostics;
using MGroup.XFEM.FEM.Mesh;

//TODO: Make sure all triangles have the same orientation. This orientation must be the same with triangles from other elements!
//      This could be done by pointing always towards a positive node. Also apply this to 2D.
//TODO: Make these intersections as smooth as the contours in ParaView
//TODO: Optimizations are possible, but may mess up readability. E.g. depending on the case, we can target specific edges that 
//      are intersected, instead of checking all of them
//TODO: For the common case, where the level set intersects the Tet4 into a triangle, there is the corner case that this triangle 
//      is extremely close to the node. In that case, it is probably safer to regard this as "Tangent". What happens if only 1 
//      or only 2 of the triangle vertices coincide with the node? 
namespace MGroup.XFEM.Geometry.LSM
{
    public class LsmTet4Interaction : ILsmElementInteraction
    {
        public (RelativePositionCurveElement relativePosition, IntersectionMesh2D intersectionMesh) FindIntersection(
            IList<int> nodeIDs, List<double[]> nodeCoords, List<double> nodeLevelSets)
        {
            var intersectionMesh = new IntersectionMesh2D();
            return (RelativePositionCurveElement.Disjoint, intersectionMesh);
        }
    }
}
