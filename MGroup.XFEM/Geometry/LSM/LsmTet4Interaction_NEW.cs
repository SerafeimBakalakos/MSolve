using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Integration.Quadratures;
using ISAAR.MSolve.Discretization.Mesh;
using System.Diagnostics;
using MGroup.XFEM.FEM.Mesh;
using ISAAR.MSolve.LinearAlgebra.Vectors;

//TODO: Remove duplicate code between this and the Tri3 version.
//TODO: Make these intersections as smooth as the contours in ParaView
//TODO: Optimizations are possible, but may mess up readability. E.g. depending on the case, we can target specific edges that 
//      are intersected, instead of checking all of them
//TODO: For the common case, where the level set intersects the Tet4 into a triangle, there is the corner case that this triangle 
//      is extremely close to the node. In that case, it is probably safer to regard this as "Tangent". What happens if only 1 
//      or only 2 of the triangle vertices coincide with the node? 
namespace MGroup.XFEM.Geometry.LSM
{
    public class LsmTet4Interaction_NEW
    {
        private IList<int> nodeIDs;
        private List<double[]> nodeCoords;
        private readonly double tolerance;

        private bool areLevelSetsAdjusted = false;
        private List<double> nodeLevelSets;

        public LsmTet4Interaction_NEW(IList<int> nodeIDs, List<double[]> nodeCoords, List<double> nodeLevelSets, double tolerance)
        {
            Debug.Assert(nodeIDs.Count == 4);
            Debug.Assert(nodeCoords.Count == 4);
            Debug.Assert(nodeLevelSets.Count == 4);
            this.nodeIDs = nodeIDs;
            this.nodeCoords = nodeCoords;
            this.nodeLevelSets = nodeLevelSets;
            this.tolerance = tolerance;
        }

        public IntersectionMesh Mesh { get; } = new IntersectionMesh(3);

        public RelativePositionCurveElement Position { get; private set; } = RelativePositionCurveElement.Disjoint;

        public void Resolve()
        {
            

            throw new NotImplementedException();
        }


    }
}
