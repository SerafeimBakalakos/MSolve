using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Logging.VTK;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.LSM;

namespace MGroup.XFEM.Plotting.Mesh
{
    public class LsmIntersectionSegmentsMesh2D : IOutputMesh
    {
        public LsmIntersectionSegmentsMesh2D(IEnumerable<LsmElementIntersection2D> intersections)
        {
            var vertices = new List<VtkPoint>();
            this.ParentElementIDsOfVertices = new List<double>();
            var cells = new List<VtkCell>();
            int vertexID = 0;
            foreach (LsmElementIntersection2D intersection in intersections)
            {
                // Intersection points are mesh vertices
                List<double[]> points = intersection.ApproximateGlobalCartesian();
                var verticesOfIntersection = new VtkPoint[points.Count];
                for (int i = 0; i < points.Count; ++i)
                {
                    var vertex = new VtkPoint(vertexID++, points[i][0], points[i][1], 0.0);
                    verticesOfIntersection[i] = vertex;
                    vertices.Add(vertex);
                    ParentElementIDsOfVertices.Add(intersection.Element.ID);
                }

                // Line segments connecting the intersection points will be mesh cells
                for (int i = 0; i < points.Count - 1; ++i)
                {
                    cells.Add(new VtkCell(CellType.Line, 
                        new VtkPoint[] { verticesOfIntersection[i], verticesOfIntersection[i+1] }));
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

        public List<double> ParentElementIDsOfVertices { get; }
    }
}
