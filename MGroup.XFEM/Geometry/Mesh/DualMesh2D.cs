using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MGroup.XFEM.Interpolation;

namespace MGroup.XFEM.Geometry.Mesh
{
    public class DualMesh2D : DualMeshBase
    {
        public DualMesh2D(double[] minCoordinates, double[] maxCoordinates, int[] numElementsFem, int[] numElementsLsm) 
            : base(2, new UniformMesh2D(minCoordinates, maxCoordinates, numElementsFem),
                  new UniformMesh2D(minCoordinates, maxCoordinates, numElementsLsm))
        {
            ElementNeighbors = FindElementNeighbors(base.multiple);
        }

        //MODIFICATION NEEDED: Delete this
        //TODO: This should use the same method for creating a List of ElementEdge that FEM Quad4 elements use.
        public (List<(int, int)> lsmNodeIDsOfEdges, List<(double[], double[])> lsmNodeCoordsNaturalOfEdges) 
            FindEdgesOfLsmElement(int[] lsmElementIdx)
        {
            int[] nodeIDs = LsmMesh.GetElementConnectivity(lsmElementIdx);
            IReadOnlyList<double[]> nodalNaturalCoords = InterpolationQuad4.UniqueInstance.NodalNaturalCoordinates; 

            var lsmNodeIDsOfEdges = new List<(int, int)>();
            var lsmNodeCoordsNaturalOfEdges = new List<(double[], double[])>();
            for (int i = 0; i < nodeIDs.Length; ++i)
            {
                int start = i;
                int end = (i + 1) % nodeIDs.Length;
                lsmNodeIDsOfEdges.Add((nodeIDs[start], nodeIDs[end]));
                lsmNodeCoordsNaturalOfEdges.Add((nodalNaturalCoords[start], nodalNaturalCoords[end]));
            }

            return (lsmNodeIDsOfEdges, lsmNodeCoordsNaturalOfEdges);
        }

        public Submesh FindLsmNodesEdgesOfFemElement(int femElementID)
        {
            int[] femElementIdx = FemMesh.GetElementIdx(femElementID);
            int[] femNodeIDs = FemMesh.GetElementConnectivity(femElementIdx);
            int[] firstFemNodeIdx = FemMesh.GetNodeIdx(femNodeIDs[0]);
            int[] firstLsmNodeIdx = MapNodeIdxFemToLsm(firstFemNodeIdx);

            //TODO: Improve the performance of the next:
            var lsmNodeIDs = new List<int>();
            var edges = new List<(int, int)>();
            for (int j = 0; j <= multiple[1]; ++j)
            {
                for (int i = 0; i <= multiple[0]; ++i)
                {
                    int[] lsmNodeIdx = { firstLsmNodeIdx[0] + i, firstLsmNodeIdx[1] + j };
                    lsmNodeIDs.Add(LsmMesh.GetNodeID(lsmNodeIdx));

                    // Horizontal edges
                    if (i > 0)
                    {
                        int last = lsmNodeIDs.Count - 1;
                        edges.Add((lsmNodeIDs[last - 1], lsmNodeIDs[last]));
                    }
                }
            }

            // Vertical edges
            int verticalEdgesStart = edges.Count;
            int stride = multiple[0] + 1;
            for (int j = 0; j < multiple[1]; ++j)
            {
                for (int i = 0; i <= multiple[0]; ++i)
                {
                    //int down = j * multiple[0] + i;
                    //int up = (j + 1) * multiple[0] + i;
                    int down = j * stride + i;
                    int up = (j + 1) * stride + i;
                    edges.Add((lsmNodeIDs[down], lsmNodeIDs[up]));
                }
            }

            // Elements //TODO: This is the same for all elements. It only needs to be defined once, perhaps in Submesh
            var elementToEdges = new List<int[]>(LsmMesh.NumElementsTotal);
            for (int j = 0; j < multiple[1]; ++j)
            {
                for (int i = 0; i < multiple[0]; ++i)
                {
                    int bottom = j * multiple[0] + i;
                    int top = (j + 1) * multiple[0] + i;
                    int left = verticalEdgesStart + j * (multiple[0] + 1) + i;
                    int right = verticalEdgesStart + j * (multiple[0] + 1) + i + 1;
                    elementToEdges.Add(new int[] { bottom, right, top, left });
                }
            }

            return new Submesh(lsmNodeIDs, edges, elementToEdges);
        }

        protected override List<int[]> ElementNeighbors { get; }

        private List<int[]> FindElementNeighbors(int[] multiple)
        {
            var elementNeighbors = new List<int[]>();
            for (int j = 0; j < multiple[1]; ++j)
            {
                for (int i = 0; i < multiple[0]; ++i)
                {
                    // Offset from the LSM element that has the same first node as the FEM element
                    int[] offset = { i, j };
                    elementNeighbors.Add(offset);
                }
            }
            return elementNeighbors;
        }

        /// <summary>
        /// Mesh entities corresponding to a FEM element.
        /// </summary>
        public class Submesh
        {
            public Submesh(List<int> lsmNodeIDs, List<(int, int)> lsmEdges, List<int[]> lsmElementToEdges)
            {
                this.LsmNodeIDs = lsmNodeIDs;
                this.LsmEdgesToNodes = lsmEdges;
                this.LsmElementToEdges = lsmElementToEdges;
            }

            public List<int> LsmNodeIDs { get; }

            public List<(int, int)> LsmEdgesToNodes { get; }

            public List<int[]> LsmElementToEdges { get; }
        }
    }
}
