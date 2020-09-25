using System;
using System.Collections.Generic;
using System.Text;
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
        }
    }
}
