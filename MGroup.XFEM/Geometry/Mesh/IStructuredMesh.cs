using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;

namespace MGroup.XFEM.Geometry.Mesh
{
    public interface IStructuredMesh
    {
        CellType CellType { get; }

        double[] MinCoordinates { get; }

        double[] MaxCoordinates { get; }

        int[] NumElements { get; }

        int NumElementsTotal { get; }

        int[] NumNodes { get; }

        int NumNodesTotal { get; }

        int GetNodeID(int[] nodeIdx);

        int[] GetNodeIdx(int nodeID);

        double[] GetNodeCoordinates(int[] nodeIdx);

        int GetElementID(int[] elementIdx);

        int[] GetElementIdx(int elementID);

        int[] GetElementConnectivity(int[] elementIdx);
    }

    public static class StructuredMeshExtensions
    {
        /// <summary>
        /// If the 2D/3D index of the node is known, then <see cref="IStructuredMesh.GetNodeCoordinates(int[])"/> is faster.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="nodeID"></param>
        public static double[] GetNodeCoordinates(this IStructuredMesh mesh, int nodeID)
            => mesh.GetNodeCoordinates(mesh.GetNodeIdx(nodeID));

        /// <summary>
        /// If the 2D/3D index of the element is known, then <see cref="IStructuredMesh.GetElementConnectivity(int[])"/> is 
        /// faster.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="elementID"></param>
        public static int[] GetElementConnectivity(this IStructuredMesh mesh, int elementID)
            => mesh.GetElementConnectivity(mesh.GetElementIdx(elementID));
    }
}
