using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using MGroup.XFEM.Geometry;

namespace MGroup.XFEM.Tests.Geometry
{
    public class IntersectedMesh3DComparer
    {
        private readonly Point3DComparer pointComparer;

        public IntersectedMesh3DComparer(double tolerance)
        {
            this.pointComparer = new Point3DComparer(tolerance);
        }

        public bool AreEqual(IntersectionMesh3D mesh0, IntersectionMesh3D mesh1)
        {
            bool verticesEqual = ArePointGroupsEqual(mesh0.Vertices, mesh1.Vertices);
            if (!verticesEqual)
            {
                return false;
            }

            // Check that each of mesh1.Cells belongs in mesh0.Cells
            if (mesh0.Cells.Count != mesh1.Cells.Count)
            {
                return false;
            }
            var remainingCells1 = Enumerable.Range(0, mesh1.Cells.Count).ToList();
            for (int c0 = 0; c0 < mesh0.Cells.Count; ++c0)
            {
                int sameCell1 = -1;
                foreach (int c1 in remainingCells1)
                {
                    if (AreCellsEqual(mesh0, c0, mesh1, c1))
                    {
                        sameCell1 = c1;
                        break;
                    }
                }

                if (sameCell1 == -1)
                {
                    return false;
                }
                else remainingCells1.Remove(sameCell1);
            }

            return true;
        }

        public bool ArePointsEqual(double[] vertex0, double[] vertex1) 
            => pointComparer.Compare(vertex0, vertex1) == 0; 

        public bool AreCellsEqual(IntersectionMesh3D mesh0, int cell0Idx, IntersectionMesh3D mesh1, int cell1Idx)
        {
            (CellType type, int[] vertices) cell0 = mesh0.Cells[cell0Idx];
            (CellType type, int[] vertices) cell1 = mesh1.Cells[cell1Idx];
            if (cell0.type != cell1.type)
            {
                return false;
            }

            var vertices0 = new List<double[]>();
            foreach (int v in cell0.vertices) vertices0.Add(mesh0.Vertices[v]);
            var vertices1 = new List<double[]>();
            foreach (int v in cell1.vertices) vertices1.Add(mesh1.Vertices[v]);
            return ArePointGroupsEqual(vertices0, vertices1);
        }

        private bool ArePointGroupsEqual(ICollection<double[]> points0, ICollection<double[]> points1)
        {
            if (points0.Count != points1.Count)
            {
                return false;
            }
            List<double[]> points0Sorted = new SortedSet<double[]>(points0, pointComparer).ToList();
            List<double[]> points1Sorted = new SortedSet<double[]>(points1, pointComparer).ToList();
            for (int i = 0; i < points0.Count; ++i)
            {
                if (pointComparer.Compare(points0Sorted[i], points1Sorted[i]) != 0)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
