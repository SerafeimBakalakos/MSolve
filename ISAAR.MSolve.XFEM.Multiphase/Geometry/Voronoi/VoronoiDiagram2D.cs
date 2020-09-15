using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;

//TODO: Verify that each edge has a pos and a neg cell
namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry.Voronoi
{
    public class VoronoiDiagram2D
    {
        public VoronoiDiagram2D(List<CartesianPoint> seeds, List<CartesianPoint> vertices, List<int[]> cells)
        {
            // Remove infinity vertex
            this.Vertices = new List<CartesianPoint>(vertices);
            this.Vertices.RemoveAt(0);

            // Keep cells without edges that go to infinity
            this.Cells = RemoveInfinityCells(cells);

            // Find which seeds correspond to these cells
            this.Seeds = RemoveInactiveSeeds(seeds, this.Vertices, this.Cells);
            Debug.Assert(this.Seeds.Count == this.Cells.Count);
            CheckSeedsCells(Seeds, Vertices, Cells);

            // Create edges
            this.Edges = CreateEdges(this.Seeds, this.Vertices, this.Cells);
        }

        public List<int[]> Cells { get; }

        //public List<CartesianPoint> CellCentroids { get; }

        public List<VoronoiEdge2D> Edges { get; }

        /// <summary>
        /// <see cref="Seeds"/>[i] is the point inside <see cref="Cells"/>[i] used as a seed for the Voroni generation.
        /// </summary>
        public List<CartesianPoint> Seeds { get; }

        public List<CartesianPoint> Vertices { get; }

        [Conditional("DEBUG")]
        private static void CheckSeedsCells(List<CartesianPoint> seeds, List<CartesianPoint> vertices, List<int[]> cells)
        {
            Debug.Assert(seeds.Count == cells.Count);
            for (int c = 0; c < seeds.Count; ++c)
            {
                var cellVertices = new List<CartesianPoint>();
                foreach (int index in cells[c]) cellVertices.Add(vertices[index]);
                var polygon = ConvexPolygon2D.CreateUnsafe(cellVertices);
                bool isInside = polygon.IsPointInsidePolygon(seeds[c]);
                Debug.Assert(isInside);
            }
        }

        private static List<CartesianPoint> CreateCellCentroids(List<CartesianPoint> vertices, List<int[]> cells)
        {
            var cellCentroids = new List<CartesianPoint>(cells.Count);
            foreach (int[] cell in cells)
            {
                int numVertices = cell.Length;
                double x = 0, y = 0;
                foreach (int index in cell)
                {
                    x += vertices[index].X;
                    y += vertices[index].Y;
                }
                cellCentroids.Add(new CartesianPoint(x / numVertices, y / numVertices));
            }
            return cellCentroids;
        }

        private static List<VoronoiEdge2D> CreateEdges(
            List<CartesianPoint> seeds, List<CartesianPoint> vertices, List<int[]> cells)
        {
            var edges = new List<VoronoiEdge2D>();
            for (int c = 0; c < cells.Count; ++c)
            {
                CartesianPoint seed = seeds[c];
                for (int v = 0; v < cells[c].Length; ++v)
                {
                    int start = cells[c][v];
                    int end = cells[c][(v + 1) % cells[c].Length];

                    // This edge may have already been created
                    VoronoiEdge2D edge = FindEdge(edges, start, end);

                    // Otherwise create a new one
                    if (edge == null)
                    {
                        edge = new VoronoiEdge2D();
                        edge.Vertices = new int[] { start, end };
                        edges.Add(edge);
                    }

                    // Associate the edge with the cell
                    var segment = new LineSegment2D(vertices[edge.Vertices[0]], vertices[edge.Vertices[1]]);
                    double distance = segment.SignedDistanceOf(seed);
                    Debug.Assert(distance != 0);
                    if (distance > 0) edge.CellPositive = c;
                    else edge.CellNegative = c;
                }
            }
            return edges;
        }

        private static VoronoiEdge2D FindEdge(List<VoronoiEdge2D> edges, int start, int end)
        {
            foreach (VoronoiEdge2D edge in edges)
            {
                bool same = (edge.Vertices[0] == start) && (edge.Vertices[1] == end);
                same |= (edge.Vertices[0] == end) && (edge.Vertices[1] == start);
                if (same) return edge;
            }
            return null;
        }

        private static List<CartesianPoint> RemoveInactiveSeeds(List<CartesianPoint> allSeeds, 
            List<CartesianPoint> vertices, List<int[]> cells)
        {
            // Create polygon outline for each cell
            var cellPolygons = new Dictionary<ConvexPolygon2D, int>();
            for (int c = 0; c < cells.Count; ++c)
            {
                var cellVertices = new List<CartesianPoint>();
                foreach (int index in cells[c]) cellVertices.Add(vertices[index]);
                var polygon = ConvexPolygon2D.CreateUnsafe(cellVertices);
                cellPolygons[polygon] = c;
            }

            var effectiveSeeds = new SortedDictionary<int, CartesianPoint>();
            foreach (CartesianPoint seed in allSeeds)
            {
                // Find the polygon that surrounds this seed
                ConvexPolygon2D surroundingCell = null;
                foreach (ConvexPolygon2D polygon in cellPolygons.Keys)
                {
                    if (polygon.IsPointInsidePolygon(seed))
                    {
                        surroundingCell = polygon;
                        break;
                    }
                }

                // Associate the cell and seed by enforcing the same index
                if (surroundingCell != null) // Ignore seeds that are in infinity cells
                {
                    int index = cellPolygons[surroundingCell];
                    effectiveSeeds[index] = seed;
                    cellPolygons.Remove(surroundingCell); // faster searching for the next seeds
                }
            }
            return effectiveSeeds.Values.ToList();
        }

        private static List<int[]> RemoveInfinityCells(List<int[]> cells)
        {
            var newCells = new List<int[]>();
            foreach (int[] cell in cells)
            {
                bool hasInfinityEdge = false;
                foreach (int index in cell)
                {
                    if (index == 0)
                    {
                        hasInfinityEdge = true;
                        break;
                    }
                }
                if (!hasInfinityEdge)
                {
                    var newCell = new int[cell.Length];
                    for (int v = 0; v < cell.Length; ++v) newCell[v] = cell[v] - 1;
                    newCells.Add(newCell);
                }
            }
            return newCells;
        }

        public class VoronoiEdge2D
        {
            public int[] Vertices { get; set; }

            public int CellNegative { get; set; } = -1;

            public int CellPositive { get; set; } = -1;
        }
    }
}
