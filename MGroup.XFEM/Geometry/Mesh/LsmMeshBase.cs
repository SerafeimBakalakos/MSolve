using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;

namespace MGroup.XFEM.Geometry.Mesh
{
    public abstract class LsmMeshBase : ILsmMesh
    {
        protected readonly int dim;
        protected readonly int[] multiple;

        protected LsmMeshBase(int dimension, IStructuredMesh femMesh, IStructuredMesh lsmMesh)
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
            return FemMesh.GetElementID(femIdx);
        }

        public int MapNodeFemToLsm(int femNodeID)
        {
            int[] femIdx = FemMesh.GetNodeIdx(femNodeID);
            var lsmIdx = new int[dim];
            for (int d = 0; d < dim; d++)
            {
                lsmIdx[d] = multiple[d] * femIdx[d];
            }
            return LsmMesh.GetNodeID(lsmIdx);
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

        /// <summary>
        /// A group of nearby LSM elements, that correspond to the same FEM element. 
        /// E.g. 2D-3x3: {0, 0}, {1, 0}, {2, 0}, {0, 1}, {1, 1}, {2, 1}, {0, 2}, {1, 2}, {2, 2}, ...
        /// E.g. 3D-2x2: {0, 0, 0}, {1, 0, 0}, {0, 1, 0}, {1, 1, 0}, {1, 0, 1}, {0, 1, 1}, {1, 1, 1}, ...
        /// </summary>
        protected abstract List<int[]> ElementNeighbors { get; } 
    }
}
