using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Logging.VTK;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Entities;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry.Voronoi;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Plotting.Mesh
{
    public class VoronoiMesh : IOutputMesh<XNode>
    {
        public VoronoiMesh(VoronoiDiagram2D diagram)
        {
            var vertices = new List<VtkPoint>();
            for (int v = 0; v < diagram.Vertices.Count; ++v)
            {
                CartesianPoint vertex = diagram.Vertices[v];
                vertices.Add(new VtkPoint(v, vertex.X, vertex.Y, vertex.Z));
            }

            var cells = new HashSet<VtkCell>();
            for (int e = 0; e < diagram.Edges.Count; ++e)
            {
                var edge = diagram.Edges[e];
                VtkPoint start = vertices[edge.Vertices[0]];
                VtkPoint end = vertices[edge.Vertices[1]];
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
