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
    public static class Triangulation3DTests
    {
        private const string outputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\2020\MeshGen3D\";

        [Fact]
        public static void TestSingleIntersection()
        {
            (List<NaturalPoint> points, CellType cellType, double volume) = CreateHexa8();

            var intersections = new List<NaturalPoint>[1];
            intersections[0] = new List<NaturalPoint>();
            intersections[0].Add(new NaturalPoint(-0.75, -1.00, +1.00));
            intersections[0].Add(new NaturalPoint(-0.25, +1.00, +1.00));
            intersections[0].Add(new NaturalPoint(+0.75, +1.00, -1.00));
            intersections[0].Add(new NaturalPoint(+0.25, -1.00, -1.00));
            points.AddRange(intersections[0]);

            var triangulator = new MIConvexHullTriangulator3D();
            triangulator.MinTetrahedronVolume = 1E-5 * volume;
            IList<TetrahedronCell3D> tetahedra = triangulator.CreateMesh(points);

            //WriteConformingMesh(tetahedra);
            //PlotIntersections(tetahedra, "singleIntersection", intersections);

            var expectedTetrahedra = new List<TetrahedronCell3D>();
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { -1, 1, -1 }, new double[] { -0.25, 1, 1 }, 
                new double[] { 0.75, 1, -1 }, new double[] { 0.25, -1, -1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { 0.25, -1, -1 }, new double[] { -0.25, 1, 1 }, 
                new double[] { 0.75, 1, -1 }, new double[] { 1, 1, 1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { 0.25, -1, -1 }, new double[] { 1, 1, 1 }, 
                new double[] { 0.75, 1, -1 }, new double[] { 1, -1, 1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { 0.25, -1, -1 }, new double[] { -1, 1, -1 }, 
                new double[] { -0.25, 1, 1 }, new double[] { -1, -1, -1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { 1, -1, 1 }, new double[] { 1, 1, 1 }, 
                new double[] { -0.25, 1, 1 }, new double[] { 0.25, -1, -1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { 0.25, -1, -1 }, new double[] { -0.75, -1, 1 }, 
                new double[] { -0.25, 1, 1 }, new double[] { 1, -1, 1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { -0.25, 1, 1 }, new double[] { -1, 1, 1 }, 
                new double[] { -0.75, -1, 1 }, new double[] { -1, 1, -1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { 1, -1, -1 }, new double[] { 1, 1, -1 }, 
                new double[] { 1, -1, 1 }, new double[] { 0.75, 1, -1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { -1, -1, -1 }, new double[] { -0.75, -1, 1 }, 
                new double[] { -0.25, 1, 1 }, new double[] { 0.25, -1, -1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { -1, 1, -1 }, new double[] { -1, -1, 1 }, 
                new double[] { -1, 1, 1 }, new double[] { -0.75, -1, 1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { -0.25, 1, 1 }, new double[] { -1, 1, -1 }, 
                new double[] { -0.75, -1, 1 }, new double[] { -1, -1, -1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { -0.75, -1, 1 }, new double[] { -1, 1, -1 }, 
                new double[] { -1, -1, 1 }, new double[] { -1, -1, -1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { 0.25, -1, -1 }, new double[] { 1, -1, 1 }, 
                new double[] { 0.75, 1, -1 }, new double[] { 1, -1, -1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { 0.75, 1, -1 }, new double[] { 1, -1, 1 }, 
                new double[] { 1, 1, 1 }, new double[] { 1, 1, -1 }));

            double tol = 1E-7;
            Assert.True(TriangulationUtilities.AreEqual(expectedTetrahedra, tetahedra, tol));
        }

        [Fact]
        public static void TestDoubleIntersection()
        {
            (List<NaturalPoint> points, CellType cellType, double volume) = CreateHexa8();

            // Intersection 1:
            var intersections = new List<NaturalPoint>[2];
            intersections[0] = new List<NaturalPoint>();
            intersections[0].Add(new NaturalPoint(-0.25, -1.00, +1.00));
            intersections[0].Add(new NaturalPoint(+0.25, +1.00, +1.00));
            intersections[0].Add(new NaturalPoint(+0.75, +1.00, -1.00));
            intersections[0].Add(new NaturalPoint(+0.25, -1.00, -1.00));
            points.AddRange(intersections[0]);

            // Intersection 2:
            intersections[1] = new List<NaturalPoint>();
            intersections[1].Add(new NaturalPoint(-1.00, +0.00, +1.00));
            intersections[1].Add(new NaturalPoint(+0.00, +1.00, +1.00));
            intersections[1].Add(new NaturalPoint(-1.00, +1.00, +0.00));
            points.AddRange(intersections[1]);

            var triangulator = new MIConvexHullTriangulator3D();
            triangulator.MinTetrahedronVolume = 1E-5 * volume;
            IList<TetrahedronCell3D> tetrahedra = triangulator.CreateMesh(points);

            //WriteConformingMesh(tetrahedra);
            //PlotIntersections(tetrahedra, "doubleIntersection", intersections);

            var expectedTetrahedra = new List<TetrahedronCell3D>();
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { -1, 0, 1 }, new double[] { -1, -1, 1 },
                new double[] { -0.25, -1, 1 }, new double[] { -1, -1, -1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { -1, -1, -1 }, new double[] { 0.25, -1, -1 },
                new double[] { -1, 0, 1 }, new double[] { -1, 1, 0 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { 0.25, -1, -1 }, new double[] { 1, -1, 1 },
                new double[] { 0.75, 1, -1 }, new double[] { 1, -1, -1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { -1, 1, 0 }, new double[] { 0.25, -1, -1 },
                new double[] { 0, 1, 1 }, new double[] { 0.75, 1, -1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { -1, 0, 1 }, new double[] { -0.25, -1, 1 },
                new double[] { 0.25, -1, -1 }, new double[] { -1, -1, -1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { 1, -1, 1 }, new double[] { 1, 1, 1 },
                new double[] { 0.25, 1, 1 }, new double[] { 0.75, 1, -1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { -1, 1, -1 }, new double[] { 0.75, 1, -1 },
                new double[] { 0.25, -1, -1 }, new double[] { -1, 1, 0 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { -1, -1, -1 }, new double[] { -1, 1, -1 },
                new double[] { 0.25, -1, -1}, new double[] { -1, 1, 0 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { 1, -1, -1 }, new double[] { 1, 1, -1 },
                new double[] { 1, 1, 1 }, new double[] { 0.75, 1, -1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { -1, 1, 1 }, new double[] { -1, 0, 1 },
                new double[] { 0, 1, 1 }, new double[] { -1, 1, 0 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { 0, 1, 1 }, new double[] { 0.75, 1, -1 },
                new double[] { 0.25, -1, -1 }, new double[] { 0.25, 1, 1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { -1, 1, 0 }, new double[] { -1, 0, 1 },
                new double[] { 0, 1, 1 }, new double[] { 0.25, -1, -1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { 0, 1, 1 }, new double[] { 0.25, -1, -1 },
                new double[] { -1, 0, 1 }, new double[] { -0.25, -1, 1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { 0.75, 1, -1 }, new double[] { 1, -1, 1 },
                new double[] { 1, 1, 1 }, new double[] { 1, -1, -1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { 0.25, -1, -1 }, new double[] { 0.25, 1, 1 },
                new double[] { 0.75, 1, -1 }, new double[] { 1, -1, 1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { 0, 1, 1 }, new double[] { 0.25, 1, 1 },
                new double[] { 0.25, -1, -1 }, new double[] { -0.25, -1, 1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { 0.25, -1, -1 }, new double[] { -0.25, -1, 1},
                new double[] { 0.25, 1, 1 }, new double[] { 1, -1, 1 }));

            double tol = 1E-7;
            Assert.True(TriangulationUtilities.AreEqual(expectedTetrahedra, tetrahedra, tol));
        }

        [Fact]
        public static void TestIntersectionThroughNodes()
        {
            (List<NaturalPoint> points, CellType cellType, double volume) = CreateHexa8();

            var intersections = new List<NaturalPoint>[1];
            intersections[0] = new List<NaturalPoint>();
            intersections[0].Add(points[4]);
            intersections[0].Add(points[5]);
            intersections[0].Add(points[2]);
            intersections[0].Add(points[3]);

            var centroid = new NaturalPoint(0, 0, 0);
            points.Add(centroid);

            var triangulator = new MIConvexHullTriangulator3D();
            triangulator.MinTetrahedronVolume = 1E-5 * volume;
            IList<TetrahedronCell3D> tetrahedra = triangulator.CreateMesh(points);

            //WriteConformingMesh(tetrahedra);
            //PlotIntersections(tetrahedra, "intersectionThroughNodes", intersections);

            var expectedTetrahedra = new List<TetrahedronCell3D>();
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { 0, 0, 0 }, new double[] { 1, 1, -1 },
                new double[] { -1, 1, -1 }, new double[] { -1, -1, -1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { -1, -1, 1 }, new double[] { 1, -1, 1 },
                new double[] { -1, 1, 1 }, new double[] { 0, 0, 0 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { -1, -1, -1 }, new double[] { -1, -1, 1 },
                new double[] { -1, 1, 1 }, new double[] { 0, 0, 0 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { 0, 0, 0 }, new double[] { -1, -1, 1 },
                new double[] { 1, -1, 1 }, new double[] { -1, -1, -1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { 1, 1, -1 }, new double[] { -1, 1, -1 },
                new double[] { -1, 1, 1 }, new double[] { 0, 0, 0 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { 1, -1, -1 }, new double[] { 1, 1, -1 },
                new double[] { 1, -1, 1 }, new double[] { 0, 0, 0 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { -1, -1, -1 }, new double[] { 1, -1, -1 },
                new double[] { 1, -1, 1 }, new double[] { 0, 0, 0 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { 0, 0, 0 }, new double[] { 1, -1, -1 },
                new double[] { 1, 1, -1 }, new double[] { -1, -1, -1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { 0, 0, 0 }, new double[] { -1, 1, -1 },
                new double[] { -1, 1, 1 }, new double[] { -1, -1, -1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { 1, -1, 1 }, new double[] { 1, 1, 1 },
                new double[] { -1, 1, 1 }, new double[] { 0, 0, 0 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { 0, 0, 0 }, new double[] { 1, 1, 1 },
                new double[] { -1, 1, 1 }, new double[] { 1, 1, -1 }));
            expectedTetrahedra.Add(new TetrahedronCell3D(
                new double[] { 0, 0, 0 }, new double[] { 1, -1, 1 },
                new double[] { 1, 1, 1 }, new double[] { 1, 1, -1 }));

            double tol = 1E-7;
            Assert.True(TriangulationUtilities.AreEqual(expectedTetrahedra, tetrahedra, tol));
        }


        private static (List<NaturalPoint> points, CellType cellType, double volume) CreateHexa8()
        {
            var points = new List<NaturalPoint>();
            points.Add(new NaturalPoint(-1, -1, -1));
            points.Add(new NaturalPoint(+1, -1, -1));
            points.Add(new NaturalPoint(+1, +1, -1));
            points.Add(new NaturalPoint(-1, +1, -1));
            points.Add(new NaturalPoint(-1, -1, +1));
            points.Add(new NaturalPoint(+1, -1, +1));
            points.Add(new NaturalPoint(+1, +1, +1));
            points.Add(new NaturalPoint(-1, +1, +1));

            double volume = 8;
            return (points, CellType.Hexa8, volume);
        }

        private static CustomMesh CreateConformingMesh(IList<TetrahedronCell3D> tetrahedra)
        {
            var mesh = new CustomMesh();
            foreach (TetrahedronCell3D tetra in tetrahedra)
            {
                int startPoint = mesh.NumOutVertices;
                var pointsOfTriangle = new VtkPoint[4];
                for (int v = 0; v < 4; ++v)
                {
                    double[] vertex = tetra.Vertices[v];
                    var point = new VtkPoint(startPoint + v, vertex[0], vertex[1], vertex[2]);
                    pointsOfTriangle[v] = point;
                    mesh.Vertices.Add(point);
                }
                mesh.Cells.Add(new VtkCell(CellType.Tet4, pointsOfTriangle));
            }
            return mesh;
        }

        private static CustomMesh CreateIntersectionMesh(IList<NaturalPoint>[] intersections)
        {
            var mesh = new CustomMesh();
            int offset = 0;
            for (int i = 0; i < intersections.Length; ++i)
            {
                IList<NaturalPoint> cell = intersections[i];
                CellType cellType;
                if (cell.Count == 3)
                {
                    cellType = CellType.Tri3;
                }
                else if (cell.Count == 4)
                {
                    cellType = CellType.Quad4;
                }
                else throw new NotImplementedException("Unknown intersection shape");

                var verticesOfCell = new List<VtkPoint>();
                for (int v = 0; v < cell.Count; ++v)
                {
                    NaturalPoint point = intersections[i][v];
                    var vertex = new VtkPoint(offset + v, point.Xi, point.Eta, point.Zeta);
                    verticesOfCell.Add(vertex);
                    mesh.Vertices.Add(vertex);
                }
                mesh.Cells.Add(new VtkCell(cellType, verticesOfCell));

                offset += cell.Count;
            }

            return mesh;
        }

        private static CustomMesh CreateOriginalMesh()
        {
            (List<NaturalPoint> points, CellType cellType, double volume) = CreateHexa8();
            var mesh = new CustomMesh();
            for (int i = 0; i < points.Count; ++i)
            {
                var point = new VtkPoint(i, points[i].X1, points[i].X2, points[i].X3);
                mesh.Vertices.Add(point);
            }
            mesh.Cells.Add(new VtkCell(cellType, mesh.Vertices.ToArray()));
            return mesh;
        }



        private static void PlotIntersections(IList<TetrahedronCell3D> tetrahedra, string outputCase,
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

            CustomMesh conformingMesh = CreateConformingMesh(tetrahedra);
            string conformingMeshPath = outputDirectory + $"{outputCase}_conformingMesh.vtk";
            using (var writer = new MGroup.XFEM.Plotting.Writers.VtkFileWriter(conformingMeshPath))
            {
                writer.WriteMesh(conformingMesh);
            }
        }

        private static void WriteConformingMesh(IList<TetrahedronCell3D> tetrahedra)
        {
            var builder = new StringBuilder();
            for (int t = 0; t < tetrahedra.Count; ++t)
            {
                TetrahedronCell3D tetra = tetrahedra[t];
                builder.AppendLine($"Tetrahedron {t}: ");
                for (int v = 0; v < tetra.Vertices.Count; ++v)
                {
                    double[] vertex = tetra.Vertices[v];
                    builder.AppendLine($"Vertex {v}: ({vertex[0]}, {vertex[1]}, {vertex[2]})");
                }
                builder.AppendLine();
            }
            Debug.WriteLine(builder.ToString());
        }
    }
}
