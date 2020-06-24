using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Logging.VTK;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.LSM;

namespace MGroup.XFEM.Plotting.Mesh
{
    public class LsmIntersectionSegmentsMesh3D : IOutputMesh
    {
        public LsmIntersectionSegmentsMesh3D(IEnumerable<LsmElementIntersection3D> intersections)
        {
            var vertices = new List<VtkPoint>();
            this.ParentElementIDsOfVertices = new List<double>();
            var cells = new List<VtkCell>();
            int vertexID = 0;
            foreach (LsmElementIntersection3D intersection in intersections)
            {
                // Vertices of the intersection mesh
                IntersectionMesh intersectionMesh = intersection.ApproximateGlobalCartesian();
                IList<double[]> intersectionPoints = intersectionMesh.GetVerticesList();
                var verticesOfIntersection = new List<VtkPoint>();
                for (int v = 0; v < intersectionPoints.Count; ++v)
                {
                    double[] point = intersectionPoints[v];
                    var vertex = new VtkPoint(vertexID++, point[0], point[1], point[2]);
                    vertices.Add(vertex);
                    verticesOfIntersection.Add(vertex);
                    ParentElementIDsOfVertices.Add(intersection.Element.ID);
                }
                
                // Cells of the intersection mesh
                for (int c = 0; c < intersectionMesh.Cells.Count; ++c)
                {
                    (CellType cellType, int[] connectivity) = intersectionMesh.Cells[c];
                    var verticesOfCell = new VtkPoint[connectivity.Length];
                    for (int v = 0; v < verticesOfCell.Length; ++v)
                    {
                        verticesOfCell[v] = verticesOfIntersection[connectivity[v]];
                    }
                    cells.Add(new VtkCell(cellType, verticesOfCell));
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
