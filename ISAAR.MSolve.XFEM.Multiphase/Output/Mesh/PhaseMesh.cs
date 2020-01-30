using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Logging.VTK;
using ISAAR.MSolve.XFEM.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM.Multiphase.Output.Mesh
{
    public class PhaseMesh<TNode> : IOutputMesh<TNode> where TNode : INode
    {
        public PhaseMesh(GeometricModel model)
        {
            var vertices = new HashSet<VtkPoint>();
            var cells = new HashSet<VtkCell>();
            int vertexID = 0;
            for (int i = 1; i < model.Phases.Count; ++i)
            {
                var phase = (ConvexPhase)(model.Phases[i]);
                var polygonVertices = new List<VtkPoint>(phase.Boundaries.Count);
                foreach (PhaseBoundary boundary in phase.Boundaries)
                {
                    CartesianPoint point = boundary.Segment.Start;
                    var vertex = new VtkPoint(vertexID++, point.X, point.Y, point.Z);
                    polygonVertices.Add(vertex);
                    vertices.Add(vertex);
                }
                cells.Add(new VtkCell(CellType.Polygon, polygonVertices));
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
