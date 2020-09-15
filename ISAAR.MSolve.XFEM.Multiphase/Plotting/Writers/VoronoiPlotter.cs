using System;
using System.Collections.Generic;
using System.Linq;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Logging.VTK;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Enrichment;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Entities;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry.Voronoi;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Plotting.Mesh;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Plotting.Writers
{
    public class VoronoiPlotter
    {
        private readonly VoronoiDiagram2D diagram;

        public VoronoiPlotter(VoronoiDiagram2D diagram)
        {
            this.diagram = diagram;
        }

        public void PlotSeeds(string path)
        {
            var seeds = new Dictionary<CartesianPoint, double>();
            for (int s = 0; s < diagram.Seeds.Count; ++s)
            {
                seeds[diagram.Seeds[s]] = s;
            }
            using (var writer = new VtkPointWriter(path))
            {
                writer.WriteScalarField("voronoi_seeds", seeds);
            }
        }

        public void PlotDiagram(string path)
        {
            using (var writer = new VtkFileWriter(path))
            {
                var voronoiMesh = new VoronoiMesh(diagram);
                writer.WriteMesh(voronoiMesh);
            }
        }
    }
}
