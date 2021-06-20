using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MGroup.Geometry.Mesh;
using MGroup.XFEM.Interpolation;

namespace MGroup.XFEM.Geometry.Mesh
{
    /// <summary>
    /// This work only for a very specific division of 1 Quad into 2 triangles.
    /// </summary>
    public class DualCartesianSimplicialMesh2D : IDualMesh
    {
        private const int dim = 2;
        private const int numSimplicesPerCartesianCell = 2;
        private readonly UniformCartesianMesh2D coarseMesh;
        private readonly UniformSimplicialMesh2D fineMesh;
        private readonly int[] multiple;
        private readonly int numFineElementsPerCoarseElement;

        private DualCartesianSimplicialMesh2D(UniformCartesianMesh2D coarseMesh, UniformSimplicialMesh2D fineMesh) 
        {
            this.coarseMesh = coarseMesh;
            this.fineMesh = fineMesh;

            multiple = new int[dim];
            for (int d = 0; d < dim; ++d)
            {
                if (fineMesh.NumNodes[d] < coarseMesh.NumNodes[d])
                {
                    throw new ArgumentException("The number of nodes in each axis of the fine mesh must be greater than"
                        + " or equal to the number of nodes in that axis of the coarse mesh");
                }
                if ((fineMesh.NumNodes[d] - 1) % coarseMesh.NumElements[d] != 0)
                {
                    throw new ArgumentException("The number of elements in each axis of the fine mesh must be a multiple of"
                        + " the number of elements in that axis of the coarse mesh");
                }
                multiple[d] = (fineMesh.NumNodes[d] - 1) / coarseMesh.NumElements[d];
            }

            numFineElementsPerCoarseElement = numSimplicesPerCartesianCell;
            for (int d = 0; d < dim; ++d)
            {
                numFineElementsPerCoarseElement *= multiple[d];
            }

            CoarseToFineElementOffsets = FindElementOffsets(multiple);
        }

        public IStructuredMesh FineMesh => fineMesh;

        public IStructuredMesh CoarseMesh => coarseMesh;

        private IIsoparametricInterpolation FineElementInterpolation => InterpolationTri3.UniqueInstance;

        /// <summary>
        /// Let {i, j} (or {i, j, k} in 3D) be the index of a coarse element. Each entry of this list is the offset of the 
        /// index of a fine cartesian element, which is included in the coarse element {i, j} (or {i, j, k} in 3D).
        /// E.g. 2D-3x3: {0, 0}, {1, 0}, {2, 0}, {0, 1}, {1, 1}, {2, 1}, {0, 2}, {1, 2}, {2, 2}.
        /// E.g. 3D-2x2: {0, 0, 0}, {1, 0, 0}, {0, 1, 0}, {1, 1, 0}, {0, 0, 1}, {1, 0, 1}, {0, 1, 1}, {1, 1, 1}.
        /// </summary>
        private List<int[]> CoarseToFineElementOffsets { get; }

        /// <summary>
        /// If the node in the fine mesh does not correspond to a node in the coarse mesh, -1 will be returned
        /// </summary>
        /// <param name="fineNodeID"></param>
        public int MapNodeFineToCoarse(int fineNodeID)
        {
            int[] fineIdx = FineMesh.GetNodeIdx(fineNodeID);
            var coarseIdx = new int[dim];
            for (int d = 0; d < dim; d++)
            {
                if (fineIdx[d] % multiple[d] != 0) return -1;
                else coarseIdx[d] = fineIdx[d] / multiple[d];
            }
            return CoarseMesh.GetNodeID(coarseIdx);
        }

        public int MapNodeIDCoarseToFine(int coarseNodeID)
        {
            int[] coarseIdx = CoarseMesh.GetNodeIdx(coarseNodeID);
            var fineIdx = MapNodeIdxCoarseToFine(coarseIdx);
            return FineMesh.GetNodeID(fineIdx);
        }

        public int[] MapNodeIdxCoarseToFine(int[] coarseNodeIdx)
        {
            var fineIdx = new int[dim];
            for (int d = 0; d < dim; d++)
            {
                fineIdx[d] = multiple[d] * coarseNodeIdx[d];
            }
            return fineIdx;
        }

        public int MapElementFineToCoarse(int fineElementID)
        {
            // The last entry is unused, since all sub-simplices belong to the same cartesian cell, defined by the first entries.
            int[] fineIdx = fineMesh.GetElementIdx(fineElementID);  
            var coarseIdx = new int[dim];
            for (int d = 0; d < dim; d++)
            {
                coarseIdx[d] = fineIdx[d] / multiple[d];
            }
            return coarseMesh.GetElementID(coarseIdx);
        }

        public int[] MapElementCoarseToFine(int coarseElementID)
        {
            int[] coarseIdx = coarseMesh.GetElementIdx(coarseElementID);
            List<int[]> elementOffsets = CoarseToFineElementOffsets;
            var fineElementIDs = new int[numFineElementsPerCoarseElement];
            int i = 0;
            foreach (int[] offset in elementOffsets)
            {
                for (int j = 0; j < numSimplicesPerCartesianCell; ++j)
                {
                    var fineIdx = new int[dim + 1];
                    for (int d = 0; d < dim; d++)
                    {
                        fineIdx[d] = multiple[d] * coarseIdx[d] + offset[d];
                    }
                    fineIdx[dim] = j;
                    int fineID = fineMesh.GetElementID(fineIdx);
                    fineElementIDs[i++] = fineID;
                }
                
            }
            return fineElementIDs;
        }

        //TODO: These mapping and its inverse must also work for points on edges of the fine and coarse mesh.
        public double[] MapPointFineNaturalToCoarseNatural(int[] fineElementIdx, double[] coordsFineNatural)
        {
            // Map from the fine triangle to the fine quad. 
            // To prove these formulas, use the interpolation from natural triangle to natural quad system.
            var coordsFineQuad = new double[2];
            if (fineElementIdx[2] == 0)
            {
                // |\
                // | \
                // |__\
                coordsFineQuad[0] = 2 * coordsFineNatural[0] - 1;
                coordsFineQuad[1] = 2 * coordsFineNatural[1] - 1;
            }
            else
            {
                Debug.Assert(fineElementIdx[2] == 1);

                // ___
                // \  |
                //  \ |
                //   \|
                coordsFineQuad[0] = 1 - 2 * coordsFineNatural[0];
                coordsFineQuad[1] = 1 - 2 * coordsFineNatural[1];
            }

            // Map from the fine quad to the coarse quad
            var coordsCoarseNatural = new double[dim];
            for (int d = 0; d < dim; ++d)
            {
                // Let: 
                // x = coarse natural coordinate
                // x0 = coarse natural coordinate starting from the min point of the axis (-1): x0 = 1 + x
                // dx = max - min of coarse natural coordinate axis: dx = +1 - (-1) = 2
                // m = multiplicity of fine elements per coarse element in this axis
                // i = index of fine element starting from the one at the min of the axis: i = 0, 1, ... m-1
                // r = fine natural coordinate
                // r0 = fine natural coordinate starting from the min point of the axis (-1): r0 = 1 + r
                // dr = max - min of fine natural coordinate axis: dr = dx/m
                // x0 = i * dr + r0/m => x = i * dx / m + (1+r) / m - 1

                int i = fineElementIdx[d] % multiple[d];
                coordsCoarseNatural[d] = (i * 2.0 + coordsFineQuad[d] + 1.0) / multiple[d] - 1.0;
            }
            return coordsCoarseNatural;
        }

        public DualMeshPoint CalcShapeFunctions(int coarseElementID, double[] coarseNaturalCoords)
        {
            // Find the quad of the fine mesh containing that point and the natural coordinates in that quad
            var subElementsIdx = new int[dim];
            var coordsFineQuad = new double[dim];
            for (int d = 0; d < dim; ++d)
            {
                // Let: 
                // x = coarse natural coordinate
                // x0 = coarse natural coordinate starting from the min point of the axis (-1): x0 = 1 + x
                // dx = max - min of coarse natural coordinate axis: dx = +1 - (-1) = 2
                // m = multiplicity of fine elements per coarse element in this axis
                // i = index of fine element starting from the one at the min of the axis: i = 0, 1, ... m-1
                // r = fine natural coordinate
                // r0 = fine natural coordinate starting from the min point of the axis (-1): r0 = 1 + r
                // dr = max - min of fine natural coordinate axis: dr = dx/m
                // x0 = i * dr + r0/m => r = m * x - 2 * i + m - 1
                double dx = 2.0; // [-1, 1]
                double x0 = 1 + coarseNaturalCoords[d];
                double m = multiple[d];
                subElementsIdx[d] = (int)Math.Floor(x0 * m / dx);
                coordsFineQuad[d] = m * coarseNaturalCoords[d] - 2 * subElementsIdx[d] + m - 1;
            }

            // Map from the fine quad to the fine triangle.
            var coordsFineTriangle = new double[2];
            int subtriangle = (coordsFineQuad[0] + coordsFineQuad[1] <= 0) ? 0 : 1;
            if (subtriangle == 0) // under (or on) the diagonal of the quad
            {
                // |\
                // | \
                // |__\
                coordsFineTriangle[0] = 0.5 * (coordsFineQuad[0] + 1);
                coordsFineTriangle[1] = 0.5 * (coordsFineQuad[1] + 1);
            }
            else // over the diagonal of the quad
            {
                // ___
                // \  |
                //  \ |
                //   \|
                coordsFineTriangle[0] = 0.5 * (1 - coordsFineQuad[0]);
                coordsFineTriangle[1] = 0.5 * (1 - coordsFineQuad[1]);
            }

            // Calculate the shape functions in this fine element
            double[] shapeFunctions = FineElementInterpolation.EvaluateFunctionsAt(coordsFineTriangle);

            var result = new DualMeshPoint();
            result.FineNaturalCoordinates = coordsFineTriangle;
            result.FineShapeFunctions = shapeFunctions;

            result.FineElementIdx = new int[dim + 1];
            int[] coarseElementIdx = coarseMesh.GetElementIdx(coarseElementID);
            for (int d = 0; d < dim; ++d)
            {
                result.FineElementIdx[d] = coarseElementIdx[d] * multiple[d] + subElementsIdx[d];
            }
            result.FineElementIdx[dim] = subtriangle;

            return result;
        }

        private List<int[]> FindElementOffsets(int[] multiple)
        {
            var offsets = new List<int[]>();
            for (int j = 0; j < multiple[1]; ++j)
            {
                for (int i = 0; i < multiple[0]; ++i)
                {
                    // Offset from the fine element that has the same first node as the coarse element
                    offsets.Add(new int[] { i, j });
                }
            }
            return offsets;
        }

        public class Builder
        {
            private readonly double[] minCoordinates;
            private readonly double[] maxCoordinates;
            private readonly int[] numNodesCoarse;
            private readonly int[] numNodesFine;

            public Builder(double[] minCoordinates, double[] maxCoordinates, int[] numNodesCoarse, int[] numNodesFine)
            {
                this.minCoordinates = minCoordinates;
                this.maxCoordinates = maxCoordinates;
                this.numNodesCoarse = numNodesCoarse;
                this.numNodesFine = numNodesFine;
            }

            public DualCartesianSimplicialMesh2D BuildMesh()
            {
                int[] numElementsCoarse = { numNodesCoarse[0] - 1, numNodesCoarse[1] - 1 };
                var coarseMesh = new UniformCartesianMesh2D.Builder(minCoordinates, maxCoordinates, numElementsCoarse)
                    .SetMajorAxis(0) //TODO: Implement the other options in the mesh class and the builder.
                    .BuildMesh();
                var fineMesh = new UniformSimplicialMesh2D.Builder(minCoordinates, maxCoordinates, numNodesFine)
                    .SetMajorAxis(0)
                    .BuildMesh();
                return new DualCartesianSimplicialMesh2D(coarseMesh, fineMesh);
            }
        }
    }
}
