using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Logging.VTK;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Entities;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry.Hexagons;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Plotting.Mesh
{
    public class HexagonalOutputMesh : IOutputMesh<XNode>
    {
        public HexagonalOutputMesh(HexagonalGrid grid)
        {
            var vertices = new List<VtkPoint>();
            for (int v = 0; v < grid.Vertices.Count; ++v)
            {
                CartesianPoint vertex = grid.Vertices[v];
                vertices.Add(new VtkPoint(v, vertex.X, vertex.Y, vertex.Z));
            }

            var cells = new HashSet<VtkCell>();
            for (int c = 0; c < grid.Cells.Count; ++c)
            {
                int[] connectivity = grid.Cells[c];
                for (int v = 0; v < connectivity.Length; ++v)
                {
                    VtkPoint start = vertices[connectivity[v]];
                    VtkPoint end = vertices[connectivity[(v + 1) % connectivity.Length]];
                    cells.Add(new VtkCell(CellType.Line, new VtkPoint[] { start, end }));
                }
            }

            OutVertices = vertices;
            NumOutVertices = vertices.Count;
            OutCells = cells;
            NumOutCells = cells.Count;
        }

        public int NumOutCells { get; }

        public int NumOutVertices { get; }

        public IEnumerable<VtkCell> OutCells { get; }

        public IEnumerable<VtkPoint> OutVertices { get; }
    }
}
