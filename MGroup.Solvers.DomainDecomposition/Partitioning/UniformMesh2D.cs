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
        private readonly int axisMajor;
        private readonly int axisMinor;
        private readonly double[] dx;
        private readonly int[][] elementNodeIdxOffsets;

        private UniformMesh2D(double[] minCoordinates, double[] maxCoordinates, int[] numElements, int axisMajor, int axisMinor)
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

            this.axisMajor = axisMajor;
            this.axisMinor = axisMinor;

            // Default order for Quad4
            elementNodeIdxOffsets = new int[4][];
            elementNodeIdxOffsets[0] = new int[] { 0, 0 };
            elementNodeIdxOffsets[1] = new int[] { 1, 0 };
            elementNodeIdxOffsets[2] = new int[] { 1, 1 };
            elementNodeIdxOffsets[3] = new int[] { 0, 1 };
        }

        public CellType CellType => CellType.Quad4;

        public double[] MinCoordinates { get; }

        public double[] MaxCoordinates { get; }

        public int[] NumElements { get; }

        public int NumElementsTotal { get; }

        public int[] NumNodes { get; }

        public int NumNodesPerElement => 4;

        public int NumNodesTotal { get; }

        /// <summary>
        /// Creates a new uniform mesh. The nodes will be ordered such that they are contiguous in the dimension with mininum 
        /// number of nodes.
        /// </summary>
        /// <param name="minCoordinates"></param>
        /// <param name="maxCoordinates"></param>
        /// <param name="numElements">Array with 2 entries, each of which must be &gt; 1.</param>
        public static UniformMesh2D Create(double[] minCoordinates, double[] maxCoordinates, int[] numElements)
        {
            int axisMajor, axisMinor;
            if (numElements[0] <= numElements[1])
            {
                axisMajor = 0;
                axisMinor = 1;
            }
            else
            {
                axisMajor = 1;
                axisMinor = 0;
            }
            return new UniformMesh2D(minCoordinates, maxCoordinates, numElements, axisMajor, axisMinor);
        }

        /// <summary>
        /// Creates a new uniform mesh. The nodes will be ordered such that they are contiguous along dimension 
        /// <paramref name="majorAxis"/>.
        /// </summary>
        /// <param name="minCoordinates"></param>
        /// <param name="maxCoordinates"></param>
        /// <param name="numElements"></param>
        /// <param name="majorAxis">Must be 0 or 1 for contiguous nodes along x or y respectively.</param>
        /// <returns></returns>
        public static UniformMesh2D Create(double[] minCoordinates, double[] maxCoordinates, int[] numElements, int majorAxis)
        {
            int minorAxis;
            if (majorAxis == 1)
            {
                minorAxis = 0;
            }
            else if (majorAxis == 0)
            {
                minorAxis = 1;
            }
            else throw new ArgumentException("Major axis must be either 0 or 1");
            return new UniformMesh2D(minCoordinates, maxCoordinates, numElements, majorAxis, minorAxis);
        }

        public IEnumerable<(int nodeID, double[] coordinates)> EnumerateNodes()
        {
            for (int j = 0; j < NumNodes[axisMinor]; ++j)
            {
                for (int i = 0; i < NumNodes[axisMajor]; ++i)
                {
                    var idx = new int[dim];
                    idx[axisMinor] = j;
                    idx[axisMajor] = i;
                    yield return (GetNodeID(idx), GetNodeCoordinates(idx));
                }
            }
        }

        public IEnumerable<(int elementID, int[] nodeIDs)> EnumerateElements()
        {
            for (int k = 0; k < NumElements[axisMinor]; ++k)
            {
                for (int i = 0; i < NumElements[axisMajor]; ++i)
                {
                    var idx = new int[dim];
                    idx[axisMinor] = k;
                    idx[axisMajor] = i;
                    yield return (GetElementID(idx), GetElementConnectivity(idx));
                }
            }
        }

        public int GetNodeID(int[] nodeIdx)
        {
            // E.g. x-major (nodes contiguous along x): id = iX + iY * numNodesX
            return nodeIdx[axisMajor] + nodeIdx[axisMinor] * NumNodes[axisMajor];
        }

        public int[] GetNodeIdx(int nodeID)
        {
            // E.g. x-major (nodes contiguous along x): iX = id % numNodesX; y = id / numNodesX;
            var idx = new int[dim];
            idx[axisMajor] = nodeID % NumNodes[axisMajor];
            idx[axisMinor] = nodeID / NumNodes[axisMajor];
            return idx;
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
            // E.g. x-major (elements contiguous along x): id = iX + iY * NumElementsX
            return elementIdx[axisMajor] + elementIdx[axisMinor] * NumElements[axisMajor];
        }

        public int[] GetElementIdx(int elementID)
        {
            // E.g. x-major (elements contiguous along x): iX = id % numNodesX; y = id / NumElementsX;
            var idx = new int[dim];
            idx[axisMajor] = elementID % NumElements[axisMajor];
            idx[axisMinor] = elementID / NumElements[axisMajor];
            return idx;
        }

        public int[] GetElementConnectivity(int[] elementIdx)
        {
            var nodeIDs = new int[4];
            var nodeIdx = new int[dim]; // Avoid allocating an array per node
            for (int n = 0; n < 4; ++n)
            {
                int[] offset = elementNodeIdxOffsets[n];
                nodeIdx[0] = elementIdx[0] + offset[0];
                nodeIdx[1] = elementIdx[1] + offset[1];
                nodeIDs[n] = GetNodeID(nodeIdx);
            }

            return nodeIDs;
        }
    }
}
