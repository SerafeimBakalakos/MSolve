using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Logging.VTK;
using ISAAR.MSolve.XFEM.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM.Multiphase.Plotting.Mesh
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
                    CartesianPoint start = boundary.Segment.Start;
                    var vertex0 = new VtkPoint(vertexID++, start.X, start.Y, start.Z);
                    vertices.Add(vertex0);

                    CartesianPoint end = boundary.Segment.End;
                    var vertex1 = new VtkPoint(vertexID++, end.X, end.Y, end.Z);
                    vertices.Add(vertex1);

                    cells.Add(new VtkCell(CellType.Line, new VtkPoint[] { vertex0, vertex1 }));
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
