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
    public class HexagonalEdgesMesh : IOutputMesh<XNode>
    {
        public HexagonalEdgesMesh(HexagonalGrid grid)
        {
            var vertices = new List<VtkPoint>();
            for (int v = 0; v < grid.Vertices.Count; ++v)
            {
                CartesianPoint vertex = grid.Vertices[v];
                vertices.Add(new VtkPoint(v, vertex.X, vertex.Y, vertex.Z));
            }

            var cells = new HashSet<VtkCell>();
            for (int e = 0; e < grid.Edges.Count; ++e)
            {
                VtkPoint start = vertices[grid.Edges[e].Vertices[0]];
                VtkPoint end = vertices[grid.Edges[e].Vertices[1]];
                cells.Add(new VtkCell(CellType.Line, new VtkPoint[] { start, end }));
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
