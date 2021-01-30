using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MGroup.XFEM.Interpolation;

namespace MGroup.XFEM.Geometry.Mesh
{
    public class DualMesh2D : DualMeshBase
    {
        public DualMesh2D(double[] minCoordinates, double[] maxCoordinates, int[] numElementsCoarse, int[] numElementsFine) 
            : base(2, new UniformMesh2D(minCoordinates, maxCoordinates, numElementsCoarse),
                  new UniformMesh2D(minCoordinates, maxCoordinates, numElementsFine))
        {
            ElementNeighbors = FindElementNeighbors(base.multiple);
        }

        public Submesh FindFineNodesEdgesOfCoarseElement(int coarseElementID)
        {
            int[] coarseElementIdx = CoarseMesh.GetElementIdx(coarseElementID);
            int[] coarseNodeIDs = CoarseMesh.GetElementConnectivity(coarseElementIdx);
            int[] firstCoarseNodeIdx = CoarseMesh.GetNodeIdx(coarseNodeIDs[0]);
            int[] firstFineNodeIdx = MapNodeIdxCoarseToFine(firstCoarseNodeIdx);

            //TODO: Improve the performance of the next:
            var fineNodeIDs = new List<int>();
            var edges = new List<(int, int)>();
            for (int j = 0; j <= multiple[1]; ++j)
            {
                for (int i = 0; i <= multiple[0]; ++i)
                {
                    int[] fineNodeIdx = { firstFineNodeIdx[0] + i, firstFineNodeIdx[1] + j };
                    fineNodeIDs.Add(FineMesh.GetNodeID(fineNodeIdx));

                    // Horizontal edges
                    if (i > 0)
                    {
                        int last = fineNodeIDs.Count - 1;
                        edges.Add((fineNodeIDs[last - 1], fineNodeIDs[last]));
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
                    edges.Add((fineNodeIDs[down], fineNodeIDs[up]));
                }
            }

            // Elements //TODO: This is the same for all elements. It only needs to be defined once, perhaps in Submesh
            var elementToEdges = new List<int[]>(FineMesh.NumElementsTotal);
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

            return new Submesh(fineNodeIDs, edges, elementToEdges);
        }

        protected override IIsoparametricInterpolation ElementInterpolation => InterpolationQuad4.UniqueInstance;

        protected override List<int[]> ElementNeighbors { get; }

        private List<int[]> FindElementNeighbors(int[] multiple)
        {
            var elementNeighbors = new List<int[]>();
            for (int j = 0; j < multiple[1]; ++j)
            {
                for (int i = 0; i < multiple[0]; ++i)
                {
                    // Offset from the LSM element that has the same first node as the coarse element
                    int[] offset = { i, j };
                    elementNeighbors.Add(offset);
                }
            }
            return elementNeighbors;
        }

        /// <summary>
        /// Mesh entities corresponding to a coarse element.
        /// </summary>
        public class Submesh
        {
            public Submesh(List<int> fineNodeIDs, List<(int, int)> fineEdges, List<int[]> fineElementToEdges)
            {
                this.FineNodeIDs = fineNodeIDs;
                this.FineEdgesToNodes = fineEdges;
                this.FineElementToEdges = fineElementToEdges;
            }

            public List<int> FineNodeIDs { get; }

            public List<(int, int)> FineEdgesToNodes { get; }

            public List<int[]> FineElementToEdges { get; }
        }
    }
}
