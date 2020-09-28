using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Output;
using ISAAR.MSolve.LinearAlgebra.Output.Formatting;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry.Hexagons;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry.Voronoi;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Plotting.Mesh;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Plotting.Writers;

namespace ISAAR.MSolve.XFEM_OLD.Tests.Multiphase.Multigrain
{
    public static class HexagonalTests
    {
        private const string outputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Hexagonal\";
        private const string pathMesh = outputDirectory + "hexagonal_mesh.vtk";
        private const string pathEdges = outputDirectory + "hexagonal_edges.vtk";
        private const string pathCentroids = outputDirectory + "hexagonal_centroids.vtk";
        private const string pathVoronoiSeeds = outputDirectory + "voronoi_seeds.txt";


        public static void PlotHexagonalGrid()
        {
            double hexSize = 0.2;
            double minX = 0.0, maxY = 3.0;
            int numCellsX = 10, numCellsY = 10;
            var hexGrid = new HexagonalGrid(hexSize, numCellsX, numCellsY, minX, maxY);

            var outMesh = new HexagonalOutputMesh(hexGrid);
            using (var writer = new VtkFileWriter(pathMesh))
            {
                writer.WriteMesh(outMesh);
            }

            var edgesMesh = new HexagonalEdgesMesh(hexGrid);
            using (var writer = new VtkFileWriter(pathEdges))
            {
                writer.WriteMesh(edgesMesh);
            }

            var centroids = new Dictionary<CartesianPoint, double>();
            for (int i = 0; i < hexGrid.Centroids.Count; ++i)
            {
                centroids[hexGrid.Centroids[i]] = i;
            }
            using (var writer = new Logging.VTK.VtkPointWriter(pathCentroids))
            {
                writer.WriteScalarField("phase_junctions", centroids);
            }
        }

        public static void GenerateVoronoiSeeds()
        {
            // Create the hexagonal grid
            var minCoords = new double[] { 0, 0 };
            var maxCoords = new double[] { 93, 93 };
            double hexagonSize = 3.1;
            var numHexagons = new int[] { 25, 25 };
            var hexGridUpperLeftCoords = new double[]
            {
                minCoords[0] - 2.08 * hexagonSize,
                maxCoords[1] + 1.15 * hexagonSize
            };
            var hexGrid = new HexagonalGrid(hexagonSize, numHexagons[0], numHexagons[1],
                hexGridUpperLeftCoords[0], hexGridUpperLeftCoords[1]);

            // Create the seeds for Voronoi diagram
            var rng = new Random(13);
            double r = 0.5 * hexagonSize;
            var voronoiSeeds = Matrix.CreateZero(hexGrid.Centroids.Count, 2);
            for (int i = 0; i < hexGrid.Centroids.Count; ++i)
            {
                double x0 = hexGrid.Centroids[i].X;
                double y0 = hexGrid.Centroids[i].Y;
                double x = x0 + r * (-1 + 2 * rng.NextDouble());
                double y = y0 + r * (-1 + 2 * rng.NextDouble());
                voronoiSeeds[i, 0] = x;
                voronoiSeeds[i, 1] = y;
            }

            // Write them to output file
            var writer = new FullMatrixWriter();
            writer.NumericFormat = new GeneralNumericFormat();
            writer.ArrayFormat = new LinearAlgebra.Output.Formatting.Array2DFormat("", "", "", "\n", ",");
            writer.WriteToFile(voronoiSeeds, pathVoronoiSeeds);
        }
    }
}
