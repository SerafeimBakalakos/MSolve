using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Logging.VTK;
using ISAAR.MSolve.XFEM.Thermal.Enrichments.Items;
using ISAAR.MSolve.XFEM.Thermal.Entities;
using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod;

namespace ISAAR.MSolve.XFEM.Thermal.Output.Enrichments
{
    public class EnrichmentPlotter
    {
        private readonly ILsmCurve2D curve;
        private readonly XModel model;
        private readonly string outputDirectory;

        public EnrichmentPlotter(XModel model, ILsmCurve2D curve, string outputDirectory)
        {
            this.model = model;
            this.curve = curve;
            this.outputDirectory = outputDirectory;
        }

        public void PlotEnrichedNodes()
        {
            // Find heaviside enriched nodes and their signed distances
            var heavisideNodes = new Dictionary<CartesianPoint, double>();
            foreach (XNode node in model.Nodes)
            {
                foreach (var enrichment in node.EnrichmentItems)
                {
                    if (enrichment.Key is ThermalInterfaceEnrichment)
                    {
                        double distance = curve.SignedDistanceOf(node);
                        heavisideNodes[node] = Math.Sign(distance);
                    }
                }
            }

            // Log heaviside enriched nodes and the signs of their level sets.
            using (var writer = new VtkPointWriter($"{outputDirectory}\\heaviside_nodes.vtk"))
            {
                writer.WriteScalarField("Heaviside_nodes", heavisideNodes);
            }
        }
    }
}
