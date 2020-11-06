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

        int[] GetElementIdx(int elementIdx);

        int[] GetElementConnectivity(int[] elementIdx);
    }
}
