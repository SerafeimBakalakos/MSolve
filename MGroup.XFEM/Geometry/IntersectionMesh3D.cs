using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.LinearAlgebra.Commons;
using MGroup.XFEM.ElementGeometry;
using MGroup.XFEM.Exceptions;
using MGroup.XFEM.Geometry.LSM.Utilities;

namespace MGroup.XFEM.Geometry
{
    public class IntersectionMesh3D : IIntersectionMesh
    {
        private const int dim = 3;

        public IntersectionMesh3D()
        {
        }

        public static IntersectionMesh3D CreateTriagleMeshForElementFace(CellType cellType, IReadOnlyList<double[]> faceNodes)
        {
            if (cellType == CellType.Tri3) return CreateSingleCellMesh(cellType, faceNodes);
            else if (cellType == CellType.Quad4)
            {
                var mesh = new IntersectionMesh3D();
                foreach (double[] point in faceNodes) mesh.Vertices.Add(point);
                double diagonal02 = Geometry.Utilities.Distance3D(faceNodes[0], faceNodes[1]);
                double diagonal13 = Geometry.Utilities.Distance3D(faceNodes[1], faceNodes[3]);
                if (diagonal02 < diagonal13)
                {
                    mesh.Cells.Add((CellType.Tri3, new int[] { 0, 1, 2 }));
                    mesh.Cells.Add((CellType.Tri3, new int[] { 0, 2, 3 }));
                }
                else
                {
                    mesh.Cells.Add((CellType.Tri3, new int[] { 0, 1, 3 }));
                    mesh.Cells.Add((CellType.Tri3, new int[] { 1, 2, 3 }));
                }
                return mesh;
            }
            else throw new NotImplementedException();
        }

        public static IntersectionMesh3D CreateMultiCellMesh3D(IList<IntersectionPoint> intersectionPoints)
        {
            var mesh = new IntersectionMesh3D();
            if (intersectionPoints.Count < 3) throw new ArgumentException("There must be at least 3 points");
            else if (intersectionPoints.Count == 3)
            {
                foreach (IntersectionPoint point in intersectionPoints) mesh.Vertices.Add(point.CoordinatesNatural);
                mesh.Cells.Add((CellType.Tri3, new int[] { 0, 1, 2 }));
            }
            else
            {
                List<IntersectionPoint> orderedPoints = OrderPoints3D(intersectionPoints);
                foreach (IntersectionPoint point in orderedPoints) mesh.Vertices.Add(point.CoordinatesNatural);

                // Create triangles that contain the first point and 2 others
                for (int j = 1; j < orderedPoints.Count - 1; ++j)
                {
                    mesh.Cells.Add((CellType.Tri3, new int[] { 0, j, j + 1 }));
                }
            }
            return mesh;
        }

        public static IntersectionMesh3D CreateSingleCellMesh(CellType cellType, IReadOnlyList<double[]> intersectionPoints)
        {
            var mesh = new IntersectionMesh3D();
            for (int i = 0; i < intersectionPoints.Count; ++i)
            {
                mesh.Vertices.Add(intersectionPoints[i]);
            }
            int[] connectivity = Enumerable.Range(0, intersectionPoints.Count).ToArray();
            mesh.Cells.Add((cellType, connectivity));
            return mesh;
        }

        public static IntersectionMesh3D JoinMeshes(Dictionary<int, IntersectionMesh3D> intersectionsOfElements)
        {
            var jointMesh = new IntersectionMesh3D();
            foreach (IntersectionMesh3D mesh in intersectionsOfElements.Values)
            {
                int startVertices = jointMesh.Vertices.Count;
                var vertexIndicesOldToNew = new int[mesh.Vertices.Count];
                var vertexIsNew = new bool[mesh.Vertices.Count];

                // Add vertices of this partial mesh to the joint mesh 
                var comparer = new ValueComparer(1E-6);
                for (int i = 0; i < mesh.Vertices.Count; ++i)
                {
                    // Check all existing vertices of the joint mesh, in case they coincide
                    int newVertexPos = -1;
                    for (int j = 0; j < startVertices; ++j) // No need to check the vertices of this partial mesh
                    {
                        if (Utilities.PointsCoincide(jointMesh.Vertices[j], mesh.Vertices[i], comparer))
                        {
                            newVertexPos = j;
                            break;
                        }
                    }

                    // If this vertex does not exist in the joint mesh, add it
                    if (newVertexPos == -1)
                    {
                        vertexIsNew[i] = true;
                        newVertexPos = jointMesh.Vertices.Count;
                        jointMesh.Vertices.Add(mesh.Vertices[i]);
                    }

                    // Note its new position
                    vertexIndicesOldToNew[i] = newVertexPos;
                }

                // Add cells of this partial mesh to the joint mesh
                foreach ((CellType cellType, int[] oldConnectivity) in mesh.Cells)
                {
                    //TODO: Check if there is already a cell of the same type that has the same vertices! 
                    //      This can happen if the LSM mesh conforms to the curve. 

                    // Each cell has an array containing the positions of its vertices in the list of mesh vertices. 
                    // This array must be updated to reflect the new positions in the list of joint mesh vertices.
                    var newConnectivity = new int[oldConnectivity.Length];
                    for (int i = 0; i < oldConnectivity.Length; ++i)
                    {
                        newConnectivity[i] = vertexIndicesOldToNew[oldConnectivity[i]];
                    }
                    jointMesh.Cells.Add((cellType, newConnectivity));
                }
            }
            return jointMesh;
        }

        public IList<(CellType, int[])> Cells { get; } = new List<(CellType, int[])>();

        public IList<double[]> Vertices { get; } = new List<double[]>();

        /// <summary>
        /// This method just gathers all cells and renumbers the vertices accordingly, 
        /// without taking intersecting cells into account. 
        /// </summary>
        /// <param name="other"></param>
        public void MergeWith(IntersectionMesh3D other)
        {
            int offset = this.Vertices.Count;
            foreach (double[] vertex in other.Vertices) this.Vertices.Add(vertex);
            foreach ((CellType cellType, int[] originalConnectivity) in other.Cells)
            {
                int[] offsetConnectivity = Utilities.OffsetArray(originalConnectivity, offset);
                this.Cells.Add((cellType, offsetConnectivity));
            }
        }

        private static List<IntersectionPoint> OrderPoints3D(IList<IntersectionPoint> intersectionPoints)
        {
            var orderedPoints = new List<IntersectionPoint>();
            var leftoverPoints = new List<IntersectionPoint>(intersectionPoints);

            // First point
            orderedPoints.Add(leftoverPoints[0]);
            leftoverPoints.RemoveAt(0);

            // Rest of the points
            while (leftoverPoints.Count > 0)
            {
                IntersectionPoint pointI = orderedPoints[orderedPoints.Count - 1];
                int j = FindPointWithCommonFace(pointI, leftoverPoints);
                if (j >= 0)
                {
                    orderedPoints.Add(leftoverPoints[j]);
                    leftoverPoints.RemoveAt(j);
                }
                else
                {
                    throw new InvalidElementGeometryIntersectionException(
                        "No other intersection point lies on the same face as the current point");
                }
            }

            // Make sure the last point and the first one lie on the same face
            IntersectionPoint firstPoint = orderedPoints[0];
            IntersectionPoint lastPoint = orderedPoints[orderedPoints.Count - 1];
            if (!Utilities.HaveCommonEntries(firstPoint.Faces, lastPoint.Faces))
            {
                throw new InvalidElementGeometryIntersectionException("The first and last point do not lie on the same face");
            }

            return orderedPoints;
        }

        private static int FindPointWithCommonFace(IntersectionPoint point, List<IntersectionPoint> leftoverPoints)
        {
            for (int j = 0; j < leftoverPoints.Count; ++j)
            {
                IntersectionPoint otherPoint = leftoverPoints[j]; 
                if (Utilities.HaveCommonEntries(point.Faces, otherPoint.Faces)) return j;
            }
            return -1;
        }
    }
}
