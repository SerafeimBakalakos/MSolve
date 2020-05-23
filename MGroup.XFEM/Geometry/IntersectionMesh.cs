using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;

//TODO: This class should have AddVertex(), AddCell() methods that facilitate the creation of the mesh.
//      They should also take care of duplicate entries
namespace MGroup.XFEM.Geometry
{
    public class IntersectionMesh
    {
        private readonly Dictionary<NaturalPoint, int> vertices = new Dictionary<NaturalPoint, int>();

        //public IEnumerable<(NaturalPoint vertex, int id)> Vertices { get; set; }

        public IList<(CellType, int[])> Cells { get; set; } = new List<(CellType, int[])>();

        public void AddVertex(NaturalPoint vertex)
        {
            if (!vertices.ContainsKey(vertex))
            {
                int id = vertices.Count;
                vertices[vertex] = id;
            }
        }

        public void AddVertices(IEnumerable<NaturalPoint> vertices)
        {
            foreach (NaturalPoint vertex in vertices) AddVertex(vertex);
        }

        public void AddCell(CellType cellType, IList<NaturalPoint> vertices)
        {
            var vertexIds = new int[vertices.Count];
            for (int i = 0; i < vertices.Count; ++i)
            {
                vertexIds[i] = this.vertices[vertices[i]];
            }
            Cells.Add((cellType, vertexIds));
        }
    }
}
