using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using TriangleNet.Geometry;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry.Voronoi
{
    public class VoronoiDiagram2D
    {
        public VoronoiDiagram2D(List<CartesianPoint> vertices, List<int[]> cells)
        {
            Vertices = vertices;
            Cells = cells;

            // Create edges
            Edges = new List<VoronoiEdge2D>();
            for (int c = 0; c < cells.Count; ++c)
            {
                for (int v = 0; v < cells[c].Length; ++v)
                {
                    int start = cells[c][v];
                    int end = cells[c][(v + 1) % cells[c].Length];

                    // This edge may have already been created
                    bool isNew = true;
                    foreach (VoronoiEdge2D edge in Edges)
                    {
                        if (edge.IsSameEdge(start, end))
                        {
                            isNew = false;
                            break;
                        }
                    }

                    if (isNew)
                    {
                        var edge = new VoronoiEdge2D();
                        edge.Vertices = new int[] { start, end };
                        //TODO: Add positive or negative cell to edge
                        Edges.Add(edge);
                    }
                    else
                    {
                        //TODO: Add positive or negative cell to edge
                    }
                }
            }
        }

        public List<int[]> Cells { get; }

        public List<VoronoiEdge2D> Edges { get; }

        public List<CartesianPoint> Vertices { get; }

        public class VoronoiEdge2D
        {
            public int[] Vertices { get; set; }

            public int CellNegative { get; set; }
            
            public int CellPositive { get; set; }

            public bool IsSameEdge(int start, int end)
            {
                bool same = (this.Vertices[0] == start) && (this.Vertices[1] == end);
                same |= (this.Vertices[0] == end) && (this.Vertices[1] == start);
                return same;
            }
        }
    }
}
