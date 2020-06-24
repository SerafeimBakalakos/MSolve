using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;

//TODO: Clean up the data structures. Also use double[] to avoid generics.
//TODO: Facilitate the transformation from natural -> cartesian
namespace MGroup.XFEM.Geometry
{
    public class IntersectionMesh
    {
        private readonly SortedDictionary<double[], int> vertices
            = new SortedDictionary<double[], int>(new Point3DComparer());

        public IList<(CellType, int[])> Cells { get; set; } = new List<(CellType, int[])>();

        public void AddVertex(double[] vertex)
        {
            if (!vertices.ContainsKey(vertex))
            {
                int id = vertices.Count;
                vertices[vertex] = id;
            }
        }

        public void AddVertices(IEnumerable<double[]> vertices)
        {
            foreach (double[] vertex in vertices) AddVertex(vertex);
        }

        public void AddCell(CellType cellType, IList<double[]> vertices)
        {
            var vertexIds = new int[vertices.Count];
            for (int i = 0; i < vertices.Count; ++i)
            {
                vertexIds[i] = this.vertices[vertices[i]];
            }
            Cells.Add((cellType, vertexIds));
        }

        public IList<double[]> GetVerticesList()
        {
            var list = new double[vertices.Count][];
            {
                foreach (var pair in vertices)
                {
                    list[pair.Value] = pair.Key;
                }
            }
            return list;
        }
    }

    public class IntersectionMesh<TPoint> 
        where TPoint: IPoint
    {
        private readonly SortedDictionary<TPoint, int> vertices 
            = new SortedDictionary<TPoint, int>(new Point3DComparer<TPoint>());

        public IList<(CellType, int[])> Cells { get; set; } = new List<(CellType, int[])>();

        public void AddVertex(TPoint vertex)
        {
            if (!vertices.ContainsKey(vertex))
            {
                int id = vertices.Count;
                vertices[vertex] = id;
            }
        }

        public void AddVertices(IEnumerable<TPoint> vertices)
        {
            foreach (TPoint vertex in vertices) AddVertex(vertex);
        }

        public void AddCell(CellType cellType, IList<TPoint> vertices)
        {
            var vertexIds = new int[vertices.Count];
            for (int i = 0; i < vertices.Count; ++i)
            {
                vertexIds[i] = this.vertices[vertices[i]];
            }
            Cells.Add((cellType, vertexIds));
        }

        public TPoint[] GetVerticesList()
        {
            var list = new TPoint[vertices.Count];
            {
                foreach (var pair in vertices)
                {
                    list[pair.Value] = pair.Key;
                }
            }
            return list;
        }
    }
}
