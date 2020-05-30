using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Logging.VTK;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Plotting.Mesh;
using MGroup.XFEM.Plotting.Writers;
using Xunit;

//TODO: add comment figures
namespace MGroup.XFEM.Tests.Triangulation
{
    public static class Triangulation2DTests
    {
        private const string outputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\2020\MeshGen\";

        [Fact]
        public static void TestSingleIntersection()
        {
            (List<NaturalPoint> points, CellType cellType) = CreatePolygon();
            double outlineArea = CalcPolygonArea(points);

            var intersections = new List<NaturalPoint>[1];
            intersections[0] = new List<NaturalPoint>();
            intersections[0].Add(new NaturalPoint(1.95, 3.505));
            intersections[0].Add(new NaturalPoint(1.7, 0.3));
            points.AddRange(intersections[0]);

            var triangulator = new MIConvexHullTriangulator2D();
            triangulator.MinTriangleArea = 1E-5 * outlineArea;
            IList<TriangleCell2D> triangles = triangulator.CreateMesh(points);

            //WriteConformingMesh(triangles);
            PlotIntersections(triangles, "singleIntersection", intersections);

            var expectedTriangles = new List<TriangleCell2D>();
            expectedTriangles.Add(new TriangleCell2D(
                new double[] { 1.95, 3.505 }, new double[] { 0, 2 }, new double[] { 1, 4 }));
            expectedTriangles.Add(new TriangleCell2D(
                new double[] { 0, 2 }, new double[] { 1.95, 3.505 }, new double[] { 1.7, 0.3 }));
            expectedTriangles.Add(new TriangleCell2D(
                new double[] { 1.7, 0.3 }, new double[] { 1.95, 3.505 }, new double[] { 5, 1.8 }));
            expectedTriangles.Add(new TriangleCell2D(
                new double[] { 1.7, 0.3 }, new double[] { 5, 1.8 }, new double[] { 2, 0 }));

            double tol = 1E-7;
            Assert.True(AreEqual(expectedTriangles, triangles, tol));
        }

        [Fact]
        public static void TestDoubleIntersection()
        {
            (List<NaturalPoint> points, CellType cellType) = CreatePolygon();
            double outlineArea = CalcPolygonArea(points);

            // Intersection 1:
            var intersections = new List<NaturalPoint>[2];
            intersections[0] = new List<NaturalPoint>();
            intersections[0].Add(new NaturalPoint(0.4, 1.6));
            intersections[0].Add(new NaturalPoint(2.75, 3.0375));
            points.AddRange(intersections[0]);

            // Intersection 2:
            intersections[1] = new List<NaturalPoint>();
            intersections[1].Add(new NaturalPoint(1.6, 0.4));
            intersections[1].Add(new NaturalPoint(3.5, 2.625));
            points.AddRange(intersections[1]);

            var triangulator = new MIConvexHullTriangulator2D();
            triangulator.MinTriangleArea = 1E-5 * outlineArea;
            IList<TriangleCell2D> triangles = triangulator.CreateMesh(points);

            //WriteConformingMesh(triangles);
            PlotIntersections(triangles, "doubleIntersection", intersections);

            var expectedTriangles = new List<TriangleCell2D>();
            expectedTriangles.Add(new TriangleCell2D(
                new double[] { 0.4, 1.6 }, new double[] { 0, 2 }, new double[] { 1, 4 }));
            expectedTriangles.Add(new TriangleCell2D(
                new double[] { 2.75, 3.0375 }, new double[] { 0.4, 1.6 }, new double[] { 1, 4 }));
            expectedTriangles.Add(new TriangleCell2D(
                new double[] { 0.4, 1.6 }, new double[] { 2.75, 3.0375 }, new double[] { 1.6, 0.4 }));
            expectedTriangles.Add(new TriangleCell2D(
                new double[] { 3.5, 2.625 }, new double[] { 1.6, 0.4 }, new double[] { 2.75, 3.0375 }));
            expectedTriangles.Add(new TriangleCell2D(
                new double[] { 2, 0 }, new double[] { 1.6, 0.4 }, new double[] { 3.5, 2.625 }));
            expectedTriangles.Add(new TriangleCell2D(
                new double[] { 3.5, 2.625 }, new double[] { 5, 1.8 }, new double[] { 2, 0 }));


            double tol = 1E-7;
            Assert.True(AreEqual(expectedTriangles, triangles, tol));
        }

        [Fact]
        public static void TestIntersectionThroughNodes()
        {
            (List<NaturalPoint> points, CellType cellType) = CreatePolygon();
            double outlineArea = CalcPolygonArea(points);

            var intersections = new List<NaturalPoint>[1];
            intersections[0] = new List<NaturalPoint>();
            intersections[0].Add(points[1]);
            intersections[0].Add(points[3]);

            var middle = new NaturalPoint(0.5 * (points[1].Xi + points[3].Xi), 0.5 * (points[1].Eta + points[3].Eta));
            points.Add(middle);

            var triangulator = new MIConvexHullTriangulator2D();
            triangulator.MinTriangleArea = 1E-5 * outlineArea;
            IList<TriangleCell2D> triangles = triangulator.CreateMesh(points);

            //WriteConformingMesh(triangles);
            PlotIntersections(triangles, "intersectionThroughNodes", intersections);

            var expectedTriangles = new List<TriangleCell2D>();
            expectedTriangles.Add(new TriangleCell2D(
                new double[] { 2.5, 1.9 }, new double[] { 0, 2 }, new double[] { 1, 4 }));
            expectedTriangles.Add(new TriangleCell2D(
                new double[] { 2, 0 }, new double[] { 0, 2 }, new double[] { 2.5, 1.9 }));
            expectedTriangles.Add(new TriangleCell2D(
                new double[] { 2.5, 1.9 }, new double[] { 5, 1.8 }, new double[] { 2, 0 }));
            expectedTriangles.Add(new TriangleCell2D(
                new double[] { 2.5, 1.9 }, new double[] { 1, 4 }, new double[] { 5, 1.8 }));

            double tol = 1E-7;
            Assert.True(AreEqual(expectedTriangles, triangles, tol));
        }


        private static bool AreEqual(double[] expected, double[] computed, double tol)
        {
            if (expected.Length != computed.Length) return false;
            var comparer = new ValueComparer(tol);
            for (int i = 0; i < expected.Length; ++i)
            {
                if (!comparer.AreEqual(expected[i], computed[i])) return false;
            }
            return true;
        }

        private static bool AreEqual(TriangleCell2D expected, TriangleCell2D computed, double tol)
        {
            if (expected.Vertices.Count != computed.Vertices.Count) return false;
            foreach (double[] computedVertex in computed.Vertices)
            {
                bool isInExpected = false;
                foreach (double[] expectedVertex in expected.Vertices)
                {
                    if (AreEqual(expectedVertex, computedVertex, tol))
                    {
                        isInExpected = true;
                        break;
                    }
                }
                if (!isInExpected) return false;
            }
            return true;
        }

        private static bool AreEqual(IList<TriangleCell2D> expected, IList<TriangleCell2D> computed, double tol)
        {
            if (expected.Count != computed.Count) return false;
            foreach (TriangleCell2D computedTriangle in computed)
            {
                bool isInExpected = false;
                foreach (TriangleCell2D expectedTriangle in expected)
                {
                    if (AreEqual(expectedTriangle, computedTriangle, tol))
                    {
                        isInExpected = true;
                        break;
                    }
                }
                if (!isInExpected) return false;
            }
            return true;
        }

        private static double CalcPolygonArea<TPoint>(List<TPoint> points) where TPoint: IPoint
        {
            double sum = 0.0;
            for (int vertexIdx = 0; vertexIdx < points.Count; ++vertexIdx)
            {
                TPoint vertex1 = points[vertexIdx];
                TPoint vertex2 = points[(vertexIdx + 1) % points.Count];
                sum += vertex1.X1 * vertex2.X2 - vertex2.X1 * vertex1.X2;
            }
            return Math.Abs(0.5 * sum); // area would be negative if vertices were in counter-clockwise order
        }

        private static (List<NaturalPoint> points, CellType cellType) CreatePolygon()
        {
            var points = new List<NaturalPoint>();
            points.Add(new NaturalPoint(2, 0));
            points.Add(new NaturalPoint(5, 1.8));
            points.Add(new NaturalPoint(1, 4));
            points.Add(new NaturalPoint(0, 2));
            return (points, CellType.Quad4);
        }

        private static CustomMesh CreateConformingMesh(IList<TriangleCell2D> triangles)
        {
            var mesh = new CustomMesh();
            foreach (TriangleCell2D triangle in triangles)
            {
                int startPoint = mesh.NumOutVertices;
                var pointsOfTriangle = new VtkPoint[3];
                for (int v = 0; v < 3; ++v)
                {
                    double[] vertex = triangle.Vertices[v];
                    var point = new VtkPoint(startPoint + v, vertex[0], vertex[1], 0.0);
                    pointsOfTriangle[v] = point;
                    mesh.Vertices.Add(point);
                }
                mesh.Cells.Add(new VtkCell(CellType.Tri3, pointsOfTriangle));
            }
            return mesh;
        }

        private static CustomMesh CreateIntersectionMesh(IList<NaturalPoint>[] intersections)
        {
            var mesh = new CustomMesh();
            int offset = 0;
            for (int i = 0; i < intersections.Length; ++i)
            {
                for (int v = 0; v < intersections[i].Count; ++v)
                {
                    NaturalPoint point = intersections[i][v];
                    mesh.Vertices.Add(new VtkPoint(offset + v, point.Xi, point.Eta, point.Zeta));
                }

                for (int c = 0; c < intersections[i].Count - 1; ++c)
                {
                    var pointsOfCell = new VtkPoint[] { mesh.Vertices[offset + c], mesh.Vertices[offset + c + 1] };
                    mesh.Cells.Add(new VtkCell(CellType.Line, pointsOfCell));
                }

                offset += intersections[i].Count;
            }

            return mesh;
        }

        private static CustomMesh CreateOriginalMesh()
        {
            (List<NaturalPoint> points, CellType cellType) = CreatePolygon();
            var mesh = new CustomMesh();
            for (int i = 0; i < points.Count; ++i)
            {
                var point = new VtkPoint(i, points[i].X1, points[i].X2, 0.0);
                mesh.Vertices.Add(point);
            }
            mesh.Cells.Add(new VtkCell(cellType, mesh.Vertices.ToArray()));
            return mesh;
        }

        

        private static void PlotIntersections(IList<TriangleCell2D> triangles, string outputCase,
            List<NaturalPoint>[] intersections)
        {
            CustomMesh originalMesh = CreateOriginalMesh();
            string originalMeshPath = outputDirectory + $"{outputCase}_originalMesh.vtk";
            using (var writer = new MGroup.XFEM.Plotting.Writers.VtkFileWriter(originalMeshPath))
            {
                writer.WriteMesh(originalMesh);
            }

            CustomMesh intersectionMesh = CreateIntersectionMesh(intersections);
            string intersectionMeshPath = outputDirectory + $"{outputCase}_intersectionMesh.vtk";
            using (var writer = new MGroup.XFEM.Plotting.Writers.VtkFileWriter(intersectionMeshPath))
            {
                writer.WriteMesh(intersectionMesh);
            }

            CustomMesh conformingMesh = CreateConformingMesh(triangles);
            string conformingMeshPath = outputDirectory + $"{outputCase}_conformingMesh.vtk";
            using (var writer = new MGroup.XFEM.Plotting.Writers.VtkFileWriter(conformingMeshPath))
            {
                writer.WriteMesh(conformingMesh);
            }
        }

        private static void WriteConformingMesh(IList<TriangleCell2D> triangles)
        {
            var builder = new StringBuilder();
            for (int t = 0; t < triangles.Count; ++t)
            {
                TriangleCell2D triangle = triangles[t];
                builder.AppendLine($"Triangle {t}: ");
                for (int v = 0; v < triangle.Vertices.Count; ++v)
                {
                    double[] vertex = triangle.Vertices[v];
                    builder.AppendLine($"Vertex {v}: ({vertex[0]}, {vertex[1]})");
                }
                builder.AppendLine();
            }
            Debug.WriteLine(builder.ToString());
        }
    }
}
