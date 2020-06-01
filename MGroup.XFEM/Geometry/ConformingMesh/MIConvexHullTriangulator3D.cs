using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using MIConvexHull;

namespace MGroup.XFEM.Geometry.ConformingMesh
{
    public class MIConvexHullTriangulator3D : ITriangulator3D
    {
        //TODO: Add precision controls as properties
        public MIConvexHullTriangulator3D()
        {
        }

        public double MinTetrahedronVolume { get; set; } = -1;

        public IList<TetrahedronCell3D> CreateMesh(IEnumerable<IPoint> points)
        {
            // Gather the vertices
            var vertices = new List<double[]>();
            foreach (IPoint point in points)
            {
                vertices.Add(new double[] { point.X1, point.X2, point.X3 });
            }

            // Call 3rd-party mesh generator
            var meshCells = Triangulation.CreateDelaunay(vertices).Cells.ToArray();

            // Repackage the triangle cells
            var tetrahedra = new List<TetrahedronCell3D>(meshCells.Length);
            for (int t = 0; t < meshCells.Length; ++t)
            {
                DefaultVertex[] verticesOfTriangle = meshCells[t].Vertices;
                Debug.Assert(verticesOfTriangle.Length == 4);
                var tetra = new TetrahedronCell3D();
                for (int v = 0; v < 4; ++v)
                {
                    tetra.Vertices[v] = verticesOfTriangle[v].Position;
                }
                tetrahedra.Add(tetra);
            }

            // Remove very small triangles
            if (MinTetrahedronVolume > 0)
            {
                tetrahedra.RemoveAll(tet => tet.CalcVolume() < MinTetrahedronVolume);
            }

            return tetrahedra;
        }
    }
}
