using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry.Voronoi;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Plotting.Mesh;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Plotting.Writers;

namespace ISAAR.MSolve.XFEM_OLD.Tests.Multiphase.Multigrain
{
    public static class VoronoiTests
    {
        private const string inputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Voronoi\input\";
        private const string outputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\Paper\Voronoi\output\";
        private const string pathInVoronoiSeeds = inputDirectory + "voronoi_seeds.txt";
        private const string pathInVoronoiVertices = inputDirectory + "voronoi_vertices.txt";
        private const string pathInVoronoiCells = inputDirectory + "voronoi_cells.txt";
        private const string pathOutVoronoiMesh = outputDirectory + "voronoi_mesh.vtk";
        private const string pathOutVoronoiVertices = outputDirectory + "voronoi_seeds.vtk";


        public static void ReadVoronoi()
        {
            var reader = new VoronoiReader2D();
            VoronoiDiagram2D voronoiDiagram = 
                reader.ReadMatlabVoronoiDiagram(pathInVoronoiSeeds, pathInVoronoiVertices, pathInVoronoiCells);
            var writer = new VoronoiPlotter(voronoiDiagram);
            writer.PlotSeeds(pathOutVoronoiVertices);
            writer.PlotDiagram(pathOutVoronoiMesh);
        }
    }
}
