using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;

namespace MGroup.Solvers.DomainDecomposition.Partitioning
{
    public class UniformMesh3D : IStructuredMesh
    {
        private const int dim = 3;
        private readonly double[] dx;

        public UniformMesh3D(double[] minCoordinates, double[] maxCoordinates, int[] numElements)
        {
            this.MinCoordinates = minCoordinates;
            this.MaxCoordinates = MaxCoordinates;
            this.NumElements = numElements;

            NumNodes = new int[dim];
            for (int d = 0; d < dim; d++)
            {
                NumNodes[d] = numElements[d] + 1;
            }

            dx = new double[dim];
            for (int d = 0; d < dim; d++)
            {
                dx[d] = (maxCoordinates[d] - minCoordinates[d]) / numElements[d];
            }

            NumNodesTotal = NumNodes[0] * NumNodes[1] * NumNodes[2];
            NumElementsTotal = NumElements[0] * NumElements[1] * NumElements[2];
        }

        public CellType CellType => CellType.Hexa8;

        public double[] MinCoordinates { get; }

        public double[] MaxCoordinates { get; }

        public int[] NumElements { get; }

        public int NumElementsTotal { get; }

        public int[] NumNodes { get; }

        public int NumNodesPerElement => 8;

        public int NumNodesTotal { get; }

        public IEnumerable<(int nodeID, double[] coordinates)> EnumerateNodes()
        {
            for (int k = 0; k < NumNodes[2]; ++k)
            {
                for (int j = 0; j < NumNodes[1]; ++j)
                {
                    for (int i = 0; i < NumNodes[0]; ++i)
                    {
                        int[] idx = { i, j, k };

                        yield return (GetNodeID(idx), GetNodeCoordinates(idx));
                    }
                }
            }
        }

        public IEnumerable<(int elementID, int[] nodeIDs)> EnumerateElements()
        {
            for (int k = 0; k < NumElements[2]; ++k)
            {
                for (int j = 0; j < NumElements[1]; ++j)
                {
                    for (int i = 0; i < NumElements[0]; ++i)
                    {
                        int[] idx = { i, j, k };
                        yield return (GetElementID(idx), GetElementConnectivity(idx));
                    }
                }
            }
        }

        public int GetNodeID(int[] nodeIdx)
        {
            return nodeIdx[0] + nodeIdx[1] * NumNodes[0] + nodeIdx[2] * NumNodes[0] * NumNodes[1];
        }

        public int[] GetNodeIdx(int nodeID)
        {
            int k = nodeID / (NumNodes[0] * NumNodes[1]);
            int mod = nodeID % (NumNodes[0] * NumNodes[1]);
            int j = mod / NumNodes[0];
            int i = mod % NumNodes[0];
            return new int[] { i, j, k };
        }

        public double[] GetNodeCoordinates(int[] nodeIdx)
        {
            var coords = new double[dim];
            for (int d = 0; d < dim; d++)
            {
                coords[d] = MinCoordinates[d] + nodeIdx[d] * dx[d];
            }
            return coords;
        }

        public int GetElementID(int[] elementIdx)
        {
            return elementIdx[0] + elementIdx[1] * NumElements[0] + elementIdx[2] * NumElements[0] * NumElements[1];
        }

        public int[] GetElementIdx(int elementID)
        {
            int k = elementID / (NumElements[0] * NumElements[1]);
            int mod = elementID % (NumElements[0] * NumElements[1]);
            int j = mod / NumElements[0];
            int i = mod % NumElements[0];
            return new int[] { i, j, k };
        }

        public int[] GetElementConnectivity(int[] elementIdx)
        {
            int first = elementIdx[0] + elementIdx[1] * NumNodes[0] + elementIdx[2] * NumNodes[0] * NumNodes[1];
            return new int[]
            {
                first,                                                      // (-1, -1, -1)
                first + 1,                                                  // ( 1, -1, -1)
                first + NumNodes[0] + 1,                                    // ( 1,  1, -1)
                first + NumNodes[0],                                        // (-1,  1, -1)
                first + NumNodes[0] * NumNodes[1],                          // (-1, -1,  1)
                first + NumNodes[0] * NumNodes[1] + 1,                      // ( 1, -1,  1)
                first + NumNodes[0] * NumNodes[1] + NumNodes[0] + 1,        // ( 1,  1,  1)
                first + NumNodes[0] * NumNodes[1] + NumNodes[0]             // (-1,  1,  1)
            };
        }
    }
}
