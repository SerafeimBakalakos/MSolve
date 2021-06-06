using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;

//TODO: Add non uniform rectilinear meshes, curvilinear (uniform or not) meshes, meshes with triangles/tetrahedra instead of 
//      quads/hexahedra, meshes with 2nd order and serendipity elements. See this for ideas: 
//      https://axom.readthedocs.io/en/develop/axom/mint/docs/sphinx/sections/mesh_types.html#particlemesh
namespace MGroup.Solvers.DomainDecomposition.Partitioning
{
    public interface IStructuredMesh
    {
        CellType CellType { get; }

        double[] MinCoordinates { get; }

        double[] MaxCoordinates { get; }

        int[] NumElements { get; }

        int NumElementsTotal { get; }

        int[] NumNodes { get; }

        int NumNodesPerElement { get; }

        int NumNodesTotal { get; }

        IEnumerable<(int nodeID, double[] coordinates)> EnumerateNodes();

        IEnumerable<(int elementID, int[] nodeIDs)> EnumerateElements();

        int GetNodeID(int[] nodeIdx);

        int[] GetNodeIdx(int nodeID);

        double[] GetNodeCoordinates(int[] nodeIdx);

        double[] GetNodeCoordinates(int nodeID) => GetNodeCoordinates(GetNodeIdx(nodeID));

        int GetElementID(int[] elementIdx);

        int[] GetElementIdx(int elementID);

        int[] GetElementConnectivity(int[] elementIdx);

        int[] GetElementConnectivity(int elementID) => GetElementConnectivity(GetElementIdx(elementID));
    }
}
