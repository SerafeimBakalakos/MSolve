using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry.Voronoi;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Plotting.Mesh;

namespace ISAAR.MSolve.XFEM_OLD.Tests.Multiphase.Multigrain
{
    public static class VoronoiTests
    {
        private const string inputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Voronoi\input\";
        private const string outputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Voronoi\output\";
        private const string pathVoronoiVertices = inputDirectory + "voronoi_vertices.txt";
        private const string pathVoronoiCells = inputDirectory + "voronoi_cells.txt";
        private const string pathVoronoiMesh = outputDirectory + "voronoi_mesh.vtk";

        public static void ReadVoronoi()
        {
            var reader = new VoronoiReader2D();
            VoronoiDiagram2D voronoiDiagram = reader.ReadMatlabVoronoiDiagram(pathVoronoiVertices, pathVoronoiCells);

            using (var writer = new XFEM_OLD.Multiphase.Plotting.Writers.VtkFileWriter(pathVoronoiMesh))
            {
                var voronoiMesh = new VoronoiMesh(voronoiDiagram);
                writer.WriteMesh(voronoiMesh);
            }
        }
    }
}
