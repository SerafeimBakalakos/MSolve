using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.XFEM.Geometry.HybridFries
{
    public class CrackSurface3D
    {
        public CrackSurface3D(IEnumerable<Vertex3D> vertices, IEnumerable<TriangleCell3D> cells)
        {
            this.Vertices = new List<Vertex3D>(vertices);
            this.Cells = new List<TriangleCell3D>(cells);
            this.Edges = new List<Edge3D>();
            CreateEdges();
        }

        public List<TriangleCell3D> Cells { get; }

        public List<Edge3D> Edges { get; }

        public List<Vertex3D> Vertices { get; }

        public void AlignCells()
        {
            // Change the order of vertices in cells, such that the normal vectors are consistent.
            // This must be done only at the beginning or not at all.
            throw new NotImplementedException();
        }

        private void CreateEdges()
        {
            Edges.Clear();
            foreach (TriangleCell3D cell in Cells)
            {
                for (int v = 0; v < cell.Vertices.Length; ++v)
                {
                    Vertex3D vertex0 = cell.Vertices[v];
                    Vertex3D vertex1 = cell.Vertices[(v + 1) % cell.Vertices.Length];

                    // Check if this edge is already listed, otherwise create it.
                    Edge3D thisEdge = null;
                    foreach (Edge3D otherEdge in Edges)
                    {
                        if (otherEdge.HasVertices(vertex0, vertex1))
                        {
                            thisEdge = otherEdge;
                            break;
                        }
                    }
                    if (thisEdge == null)
                    {
                        // This makes sure that edges on the boundary of the surface mesh, have the same orientation as the cells
                        thisEdge = new Edge3D(vertex0, vertex1); 
                        Edges.Add(thisEdge);
                    }
                }
            }
        }

    }
}
