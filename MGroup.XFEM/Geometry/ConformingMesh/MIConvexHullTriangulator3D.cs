using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Geometry.Primitives;
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

        public IList<Tetrahedron3D> CreateMesh(IEnumerable<double[]> points)
        {
            // Gather the vertices
            List<double[]> vertices = points.ToList();

            // Call 3rd-party mesh generator
            var meshCells = Triangulation.CreateDelaunay(vertices).Cells.ToArray();

            // Repackage the triangle cells
            var tetrahedra = new List<Tetrahedron3D>(meshCells.Length);
            for (int t = 0; t < meshCells.Length; ++t)
            {
                DefaultVertex[] verticesOfTriangle = meshCells[t].Vertices;
                Debug.Assert(verticesOfTriangle.Length == 4);
                var tetra = new Tetrahedron3D();
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
