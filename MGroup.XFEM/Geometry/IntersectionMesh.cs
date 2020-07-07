using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Elements;

namespace MGroup.XFEM.Geometry
{
    public class IntersectionMesh : IIntersectionMesh
    {
        public IntersectionMesh()
        {
        }

        public static IntersectionMesh CreateMultiCellMesh3D(Dictionary<double[], HashSet<ElementFace>> intersectionPoints)
        {
            var mesh = new IntersectionMesh();
            if (intersectionPoints.Count < 3) throw new ArgumentException("There must be at least 3 points");
            else if (intersectionPoints.Count == 3)
            {
                foreach (double[] point in intersectionPoints.Keys) mesh.Vertices.Add(point);
                mesh.Cells.Add((CellType.Tri3, new int[] { 0, 1, 2 }));
            }
            else
            {
                List<double[]> orderedPoints = OrderPoints3D(intersectionPoints);
                foreach (double[] point in orderedPoints) mesh.Vertices.Add(point);

                // Create triangles that contain the first points and 2 others
                for (int j = 1; j < orderedPoints.Count - 1; ++j)
                {
                    mesh.Cells.Add((CellType.Tri3, new int[] { 0, j, j + 1 }));
                }
            }
            return mesh;
        }

        public static IntersectionMesh CreateSingleCellMesh(CellType cellType, IList<double[]> intersectionPoints)
        {
            var mesh = new IntersectionMesh();
            for (int i = 0; i < intersectionPoints.Count; ++i)
            {
                mesh.Vertices.Add(intersectionPoints[i]);
            }
            int[] connectivity = Enumerable.Range(0, intersectionPoints.Count).ToArray();
            mesh.Cells.Add((cellType, connectivity));
            return mesh;
        }

        public IList<(CellType, int[])> Cells { get; } = new List<(CellType, int[])>();

        public IList<double[]> Vertices { get; } = new List<double[]>();

        private static List<double[]> OrderPoints3D(Dictionary<double[], HashSet<ElementFace>> facesOfPoints)
        {
            var orderedPoints = new List<double[]>();
            List<double[]> leftoverPoints = facesOfPoints.Keys.ToList();

            // First point
            orderedPoints.Add(leftoverPoints[0]);
            leftoverPoints.RemoveAt(0);

            // Rest of the points
            while (leftoverPoints.Count > 0)
            {
                double[] pointI = orderedPoints[orderedPoints.Count - 1];
                HashSet<ElementFace> phasesI = facesOfPoints[pointI];
                int j = FindPointWithCommonFace(phasesI, leftoverPoints, facesOfPoints);
                if (j >= 0)
                {
                    orderedPoints.Add(leftoverPoints[j]);
                    leftoverPoints.RemoveAt(j);
                }
                else
                {
                    throw new Exception("No other intersection point lies on the same face as the current point");
                }
            }

            // Make sure the last point and the first one lie on the same face
            var facesFirst = facesOfPoints[orderedPoints[0]];
            var facesLast = facesOfPoints[orderedPoints[orderedPoints.Count - 1]];
            if (!HaveCommonEntries(facesFirst, facesLast))
            {
                throw new Exception("The first and last point do not lie on the same face");
            }

            return orderedPoints;
        }

        private static int FindPointWithCommonFace(HashSet<ElementFace> phasesI, List<double[]> leftoverPoints, 
            Dictionary<double[], HashSet<ElementFace>> facesOfPoints)
        {
            for (int j = 0; j < leftoverPoints.Count; ++j)
            {
                HashSet<ElementFace> phasesJ = facesOfPoints[leftoverPoints[j]];
                if (HaveCommonEntries(phasesI, phasesJ)) return j;
            }
            return -1;
        }

        private static bool HaveCommonEntries(HashSet<ElementFace> facesSet0, HashSet<ElementFace> facesSet1)
        {
            foreach (var entry in facesSet0)
            {
                if (facesSet1.Contains(entry)) return true;
            }
            return false;
        }
    }
}
