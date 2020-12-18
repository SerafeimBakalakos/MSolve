using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.XFEM.Interpolation;

namespace MGroup.XFEM.Geometry.Mesh
{
    public abstract class DualMeshBase : IDualMesh
    {
        protected readonly int dim;
        protected readonly int[] multiple;

        protected DualMeshBase(int dimension, IStructuredMesh femMesh, IStructuredMesh lsmMesh)
        {
            this.dim = dimension;
            this.FemMesh = femMesh;
            this.LsmMesh = lsmMesh;

            multiple = new int[dim];
            for (int d = 0; d < dim; ++d)
            {
                if (lsmMesh.NumElements[d] % femMesh.NumElements[d] != 0)
                {
                    throw new ArgumentException("The number of elements in each axis of the LSM mesh must be a multiple of"
                        + " the number of elements in that axis of the FEM mesh");
                }
                multiple[d] = lsmMesh.NumElements[d] / femMesh.NumElements[d];
            }
        }

        public IStructuredMesh LsmMesh { get; }

        public IStructuredMesh FemMesh { get; }

        /// <summary>
        /// If the node in the LSM mesh does not correspond to a node in the FEM mesh, -1 will be returned
        /// </summary>
        /// <param name="lsmNodeID"></param>
        public int MapNodeLsmToFem(int lsmNodeID)
        {
            int[] lsmIdx = LsmMesh.GetNodeIdx(lsmNodeID);
            var femIdx = new int[dim];
            for (int d = 0; d < dim; d++)
            {
                if (lsmIdx[d] % multiple[d] != 0) return -1;
                else femIdx[d] = lsmIdx[d] / multiple[d];
            }
            return FemMesh.GetNodeID(femIdx);
        }

        public int MapNodeIDFemToLsm(int femNodeID)
        {
            int[] femIdx = FemMesh.GetNodeIdx(femNodeID);
            var lsmIdx = MapNodeIdxFemToLsm(femIdx);
            return LsmMesh.GetNodeID(lsmIdx);
        }

        public int[] MapNodeIdxFemToLsm(int[] femNodeIdx)
        {
            var lsmIdx = new int[dim];
            for (int d = 0; d < dim; d++)
            {
                lsmIdx[d] = multiple[d] * femNodeIdx[d];
            }
            return lsmIdx;
        }

        public int MapElementLsmToFem(int lsmElementID)
        {
            int[] lsmIdx = LsmMesh.GetElementIdx(lsmElementID);
            var femIdx = new int[dim];
            for (int d = 0; d < dim; d++)
            {
                femIdx[d] = lsmIdx[d] / multiple[d];
            }
            return FemMesh.GetElementID(femIdx);
        }

        public int[] MapElementFemToLsm(int femElementID)
        {
            int[] femIdx = FemMesh.GetElementIdx(femElementID);
            List<int[]> elementNeighbors = ElementNeighbors;
            var lsmElementIDs = new int[elementNeighbors.Count];
            for (int i = 0; i < elementNeighbors.Count; ++i)
            {
                // Offset from the LSM element that has the same first node as the FEM one
                int[] offset = elementNeighbors[i];
                var lsmIdx = new int[dim];
                for (int d = 0; d < dim; d++)
                {
                    lsmIdx[d] = multiple[d] * femIdx[d] + offset[d];
                }
                int lsmID = LsmMesh.GetElementID(lsmIdx);
                lsmElementIDs[i] = lsmID;
            }
            return lsmElementIDs;
        }

        public double[] MapPointLsmNaturalToFemNatural(int[] lsmElementIdx, double[] coordsLsmNatural)
        {
            var coordsFemNatural = new double[dim];
            for (int d = 0; d < dim; ++d)
            {
                // Let: 
                // x = FEM natural coordinate
                // x0 = FEM natural coordinate starting from the min point of the axis (-1): x0 = 1 + x
                // dx = max - min of FEM natural coordinate axis: dx = +1 - (-1) = 2
                // m = multiplicity of LSM elements per FEM element in this axis
                // i = index of LSM element starting from the one at the min of the axis: i = 0, 1, ... m-1
                // r = LSM natural coordinate
                // r0 = LSM natural coordinate starting from the min point of the axis (-1): r0 = 1 + r
                // dr = max - min of LSM natural coordinate axis: dr = dx/m
                // x0 = i * dr + r0/m => x = i * dx / m + (1+r) / m - 1

                int i = lsmElementIdx[d] % multiple[d];
                coordsFemNatural[d] = (i * 2.0 + coordsLsmNatural[d] + 1.0) / multiple[d] - 1.0;
            }
            return coordsFemNatural;
        }

        //TODO: Perhaps split this into a function that maps FEM -> LSM and one that calculates shape functions. 
        public DualMeshPoint CalcShapeFunctions(int femElementID, double[] femNaturalCoords)
        {
            // Find the LSM element containing that point and the natural coordinates in that element
            var subElementsIdx = new int[dim];
            var lsmNaturalCoords = new double[dim];
            for (int d = 0; d < dim; ++d)
            {
                // Let: 
                // x = FEM natural coordinate
                // x0 = FEM natural coordinate starting from the min point of the axis (-1): x0 = 1 + x
                // dx = max - min of FEM natural coordinate axis: dx = +1 - (-1) = 2
                // m = multiplicity of LSM elements per FEM element in this axis
                // i = index of LSM element starting from the one at the min of the axis: i = 0, 1, ... m-1
                // r = LSM natural coordinate
                // r0 = LSM natural coordinate starting from the min point of the axis (-1): r0 = 1 + r
                // dr = max - min of LSM natural coordinate axis: dr = dx/m
                // x0 = i * dr + r0/m => r = m * x - 2 * i + m - 1
                double dx = 2.0; // [-1, 1]
                double x0 = 1 + femNaturalCoords[d];
                double m = multiple[d];
                subElementsIdx[d] = (int)Math.Floor(x0 * m / dx);
                lsmNaturalCoords[d] = m * femNaturalCoords[d] - 2 * subElementsIdx[d] + m - 1;
            }

            // Calculate the shape functions in this LSM element
            double[] shapeFunctions = ElementInterpolation.EvaluateFunctionsAt(lsmNaturalCoords);

            var result = new DualMeshPoint();
            result.LsmNaturalCoordinates = lsmNaturalCoords;
            result.LsmShapeFunctions = shapeFunctions;

            result.LsmElementIdx = new int[dim];
            int[] femElementIdx = FemMesh.GetElementIdx(femElementID);
            for (int d = 0; d < dim; ++d)
            {
                result.LsmElementIdx[d] = femElementIdx[d] * multiple[d] + subElementsIdx[d];
            }

            return result;
        }

        protected abstract IIsoparametricInterpolation ElementInterpolation { get; }

        /// <summary>
        /// A group of nearby LSM elements, that correspond to the same FEM element. 
        /// E.g. 2D-3x3: {0, 0}, {1, 0}, {2, 0}, {0, 1}, {1, 1}, {2, 1}, {0, 2}, {1, 2}, {2, 2}, ...
        /// E.g. 3D-2x2: {0, 0, 0}, {1, 0, 0}, {0, 1, 0}, {1, 1, 0}, {1, 0, 1}, {0, 1, 1}, {1, 1, 1}, ...
        /// </summary>
        protected abstract List<int[]> ElementNeighbors { get; } 
    }
}
