using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;

namespace MGroup.Solvers.DomainDecomposition.Partitioning
{
    public class UniformMesh3D : IStructuredMesh
    {
        private const int dim = 3;
        private readonly int axisMajor;
        private readonly int axisMedium;
        private readonly int axisMinor;
        private readonly double[] dx;
        private readonly int[][] elementNodeIdxOffsets;

        private UniformMesh3D(double[] minCoordinates, double[] maxCoordinates, int[] numElements, 
            int axisMajor, int axisMedium, int axisMinor)
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

            this.axisMajor = axisMajor;
            this.axisMedium = axisMedium;
            this.axisMinor = axisMinor;

            // Default order for Hexa8
            elementNodeIdxOffsets = new int[8][];
            elementNodeIdxOffsets[0] = new int[] { 0, 0, 0 };
            elementNodeIdxOffsets[1] = new int[] { 1, 0, 0 };
            elementNodeIdxOffsets[2] = new int[] { 1, 1, 0 };
            elementNodeIdxOffsets[3] = new int[] { 0, 1, 0 };
            elementNodeIdxOffsets[4] = new int[] { 0, 0, 1 };
            elementNodeIdxOffsets[5] = new int[] { 1, 0, 1 };
            elementNodeIdxOffsets[6] = new int[] { 1, 1, 1 };
            elementNodeIdxOffsets[7] = new int[] { 0, 1, 1 };
        }

        public CellType CellType => CellType.Hexa8;

        public double[] MinCoordinates { get; }

        public double[] MaxCoordinates { get; }

        public int[] NumElements { get; }

        public int NumElementsTotal { get; }

        public int[] NumNodes { get; }

        public int NumNodesPerElement => 8;

        public int NumNodesTotal { get; }

        /// <summary>
        /// Creates a new uniform mesh. The nodes will be ordered such that they are contiguous in the dimension with mininum 
        /// number of nodes and have maximum id difference in the dimension with maximum number of nodes.
        /// </summary>
        /// <param name="minCoordinates"></param>
        /// <param name="maxCoordinates"></param>
        /// <param name="numElements">Array with 2 entries, each of which must be &gt; 1.</param>
        public static UniformMesh3D Create(double[] minCoordinates, double[] maxCoordinates, int[] numElements)
        {
            // Sort axes based on their number of elements
            var entries = new List<(int count, int axis)>();
            entries.Add((numElements[0], 0));
            entries.Add((numElements[1], 1));
            entries.Add((numElements[2], 2));
            int[] sortedAxes = SortAxes(entries);

            int axisMajor = sortedAxes[0];
            int axisMedium = sortedAxes[1]; 
            int axisMinor = sortedAxes[2];

            CheckAxes(axisMajor, axisMedium, axisMinor);
            return new UniformMesh3D(minCoordinates, maxCoordinates, numElements, axisMajor, axisMedium, axisMinor);
        }

        /// <summary>
        /// Creates a new uniform mesh. The nodes will be ordered such that they are contiguous along dimension 
        /// <paramref name="majorAxis"/>, while they will have the maximum id difference along dimension 
        /// <paramref name="minorAxis"/>.
        /// </summary>
        /// <param name="minCoordinates"></param>
        /// <param name="maxCoordinates"></param>
        /// <param name="numElements"></param>
        /// <param name="majorAxis">Must be 0, 1 or 2 for contiguous nodes along x, y or z respectively.</param>
        /// <param name="majorAxis">Must be 0, 1 or 2 for maximum node id difference nodes along x, y or z respectively.</param>
        public static UniformMesh3D Create(double[] minCoordinates, double[] maxCoordinates, int[] numElements, 
            int majorAxis, int minorAxis)
        {
            int mediumAxis;
            if ((majorAxis == 0) && (minorAxis == 1)) mediumAxis = 2;
            else if ((majorAxis == 0) && (minorAxis == 2)) mediumAxis = 1;
            else if ((majorAxis == 1) && (minorAxis == 0)) mediumAxis = 2;
            else if ((majorAxis == 1) && (minorAxis == 2)) mediumAxis = 0;
            else if ((majorAxis == 2) && (minorAxis == 0)) mediumAxis = 1;
            else if ((majorAxis == 2) && (minorAxis == 1)) mediumAxis = 0;
            else throw new ArgumentException("Major and minors axes must be 0, 1 or 2 and different from each other");
            return new UniformMesh3D(minCoordinates, maxCoordinates, numElements, majorAxis, mediumAxis, minorAxis);
        }

        public IEnumerable<(int nodeID, double[] coordinates)> EnumerateNodes()
        {
            for (int k = 0; k < NumNodes[axisMinor]; ++k)
            {
                for (int j = 0; j < NumNodes[axisMedium]; ++j)
                {
                    for (int i = 0; i < NumNodes[axisMajor]; ++i)
                    {
                        var idx = new int[dim];
                        idx[axisMinor] = k;
                        idx[axisMedium] = j;
                        idx[axisMajor] = i;

                        yield return (GetNodeID(idx), GetNodeCoordinates(idx));
                    }
                }
            }
        }

        public IEnumerable<(int elementID, int[] nodeIDs)> EnumerateElements()
        {
            for (int k = 0; k < NumElements[axisMinor]; ++k)
            {
                for (int j = 0; j < NumElements[axisMedium]; ++j)
                {
                    for (int i = 0; i < NumElements[axisMajor]; ++i)
                    {
                        var idx = new int[dim];
                        idx[axisMinor] = k;
                        idx[axisMedium] = j;
                        idx[axisMajor] = i;

                        yield return (GetElementID(idx), GetElementConnectivity(idx));
                    }
                }
            }
        }

        public int GetNodeID(int[] nodeIdx)
        {
            // E.g. x-major, y-medium, z-minor: id = iX + iY * numNodesX + iZ * NumNodesX * NumNodesY
            return nodeIdx[axisMajor] + nodeIdx[axisMedium] * NumNodes[axisMajor] 
                + nodeIdx[axisMinor] * NumNodes[axisMajor] * NumNodes[axisMedium];
        }

        public int[] GetNodeIdx(int nodeID)
        {
            int numNodesPlane = NumNodes[axisMajor] * NumNodes[axisMedium];
            int mod = nodeID % numNodesPlane;

            var idx = new int[dim];
            idx[axisMinor] = nodeID / numNodesPlane;
            idx[axisMedium] = mod / NumNodes[axisMajor];
            idx[axisMajor] = mod % NumNodes[axisMajor];

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
            // E.g. x-major, y-medium, z-minor: id = iX + iY * NumElementsX + iZ * NumElementsX * NumElementsY
            return elementIdx[axisMajor] + elementIdx[axisMedium] * NumElements[axisMajor]
                + elementIdx[axisMinor] * NumElements[axisMajor] * NumElements[axisMedium];
        }

        public int[] GetElementIdx(int elementID)
        {
            int numElementsPlane = NumElements[axisMajor] * NumElements[axisMedium];
            int mod = elementID % numElementsPlane;

            var idx = new int[dim];
            idx[axisMinor] = elementID / numElementsPlane;
            idx[axisMedium] = mod / NumElements[axisMajor];
            idx[axisMajor] = mod % NumElements[axisMajor];

            return idx;
        }

        public int[] GetElementConnectivity(int[] elementIdx)
        {
            var nodeIDs = new int[8];
            var nodeIdx = new int[dim]; // Avoid allocating an array per node
            for (int n = 0; n < 8; ++n)
            {
                int[] offset = elementNodeIdxOffsets[n];
                nodeIdx[0] = elementIdx[0] + offset[0];
                nodeIdx[1] = elementIdx[1] + offset[1];
                nodeIdx[2] = elementIdx[2] + offset[2];
                nodeIDs[n] = GetNodeID(nodeIdx);
            }

            return nodeIDs;
        }

        [Conditional("DEBUG")]
        private static void CheckAxes(int axisMajor, int axisMedium, int axisMinor)
        {
            if ((axisMajor != 0) && (axisMajor != 1) && (axisMajor != 2 ))
            {
                throw new ArgumentException("Major axis must be 0, 1 or 2");
            }
            if ((axisMedium != 0) && (axisMedium != 1) && (axisMedium != 2))
            {
                throw new ArgumentException("Medium axis must be 0, 1 or 2");
            }
            if ((axisMinor != 0) && (axisMinor != 1) && (axisMinor != 2))
            {
                throw new ArgumentException("Major axis must be 0, 1 or 2");
            }

            if ((axisMajor == axisMedium) || (axisMajor == axisMinor))
            {
                throw new ArgumentException("Major axis must be unique");
            }
            if ((axisMedium == axisMajor) || (axisMedium == axisMinor))
            {
                throw new ArgumentException("Medium axis must be unique");
            }
            if ((axisMinor == axisMajor) || (axisMinor == axisMedium))
            {
                throw new ArgumentException("Minor axis must be unique");
            }
        }

        private static int[] SortAxes(List<(int count, int axis)> entries)
        {
            Debug.Assert(entries.Count == 3);
            var sortedAxes = new int[3];
            int idx = 0;
            while (idx < 3)
            {
                int min = int.MaxValue;
                int axisOfMin = -1;

                foreach ((int count, int axis) in entries)
                {
                    if (count < min)
                    {
                        min = count;
                        axisOfMin = axis;
                    }
                    else if (count == min) // prefer axis x over y, z and axis y over z
                    {
                        if (axis < axisOfMin)
                        {
                            min = count;
                            axisOfMin = axis;
                        }
                    }
                }

                sortedAxes[idx++] = axisOfMin;
                bool removedEntry = entries.Remove((min, axisOfMin));
                Debug.Assert(removedEntry);
            }
            return sortedAxes;
        }
    }
}
