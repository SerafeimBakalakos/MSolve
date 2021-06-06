using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;

//TODOMesh: test with nonsymmetric numElements and various major, minor axes. Visualize to make sure that they are correct, 
//      write to file, use that to write tests. Repeat for 3D.
namespace MGroup.Solvers.DomainDecomposition.Partitioning
{
    public class UniformMesh2D : IStructuredMesh
    {
        private const int dim = 2;
        private readonly double[] dx;

        public UniformMesh2D(double[] minCoordinates, double[] maxCoordinates, int[] numElements)
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

            NumNodesTotal = NumNodes[0] * NumNodes[1];
            NumElementsTotal = NumElements[0] * NumElements[1];
        }

        public CellType CellType => CellType.Quad4;

        public double[] MinCoordinates { get; }

        public double[] MaxCoordinates { get; }

        public int[] NumElements { get; }

        public int NumElementsTotal { get; }

        public int[] NumNodes { get; }

        public int NumNodesPerElement => 4;

        public int NumNodesTotal { get; }

        public IEnumerable<(int nodeID, double[] coordinates)> EnumerateNodes()
        {
            for (int j = 0; j < NumNodes[1]; ++j)
            {
                for (int i = 0; i < NumNodes[0]; ++i)
                {
                    int[] idx = { i, j };
                   
                    yield return (GetNodeID(idx), GetNodeCoordinates(idx));
                }
            }
        }

        public IEnumerable<(int elementID, int[] nodeIDs)> EnumerateElements()
        {
            for (int j = 0; j < NumElements[1]; ++j)
            {
                for (int i = 0; i < NumElements[0]; ++i)
                {
                    int[] idx = { i, j };
                    yield return (GetElementID(idx), GetElementConnectivity(idx));
                }
            }
        }

        public int GetNodeID(int[] nodeIdx)
        {
            return nodeIdx[0] + nodeIdx[1] * NumNodes[0];
        }

        public int[] GetNodeIdx(int nodeID)
        {
            return new int[] { nodeID % NumNodes[0], nodeID / NumNodes[0] };
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
            return elementIdx[0] + elementIdx[1] * NumElements[0];
        }

        public int[] GetElementIdx(int elementID)
        {
            return new int[] { elementID % NumElements[0], elementID / NumElements[0] };
        }

        public int[] GetElementConnectivity(int[] elementIdx)
        {
            int first = elementIdx[0] + elementIdx[1] * NumNodes[0];
            int last = first + NumNodes[0];
            return new int[] { first, first + 1, last + 1, last };
        }
    }
}
