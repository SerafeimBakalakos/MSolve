using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;

//TODO: Perhaps a similar interface for triangular structured meshes. The generalization of triangle is called simplex so perhaps 
//      ISimplicialStructuredMesh. There is actually a lot of math behind these: https://en.wikipedia.org/wiki/Simplex.
namespace MGroup.Geometry.Mesh
{
    /// <summary>
    /// Special case of structured mesh, where elements are quadrilaterals in 2D or hexahedrals in 3D.
    /// </summary>
    public interface ICartesianMesh : IStructuredMesh
    {
        int[] NumElements { get; }

        //TODO: This can be defined for structured meshes with Tri3/Tet4. The index will not be just the cartesian coords,
        //      but also an extra integer that indicates which subcell (e.g. in Quad4 there are 2 subtriangles with indices 0 and 1).  
        //      I could either keep 2 interfaces to specify that these indices are very different or have a general method with
        //      whatever index the implementation uses and more specific methods e.g. IStructuredMesh.GetElementID(int[] index),
        //      ICartesianMesh.GetElementID(int[] cartesianIndex), ISimplicialMesh.GetElementID(int[] augmentedIndex). Frankly this
        //      is very similar to grouping int[2] and int[3] indices for 2D and 3 implementations. 
        int GetElementID(int[] elementIdx);
              
        int[] GetElementIdx(int elementID); 

        int[] GetElementConnectivity(int[] elementIdx);
    }
}
