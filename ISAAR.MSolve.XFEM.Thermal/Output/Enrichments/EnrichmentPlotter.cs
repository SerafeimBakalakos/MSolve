using System.Collections.Generic;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Logging.VTK;
using ISAAR.MSolve.XFEM.Thermal.Enrichments.Items;
using ISAAR.MSolve.XFEM.Thermal.Entities;

namespace ISAAR.MSolve.XFEM.Thermal.Output.Enrichments
{
    public class EnrichmentPlotter
    {
        private readonly GeometricModel2D geometricModel;
        private readonly XModel model;
        private readonly string outputDirectory;

        public EnrichmentPlotter(XModel model, GeometricModel2D geometricModel, string outputDirectory)
        {
            this.model = model;
            this.geometricModel = geometricModel;
            this.outputDirectory = outputDirectory.Trim('\\'); ;
        }

        public void PlotEnrichedNodes()
        {
            Dictionary<IEnrichmentItem, HashSet<XNode>> classifiedNodes = ClassifyEnrichedNodes();
            int enrichmentID = 0;
            foreach (var category in classifiedNodes)
            {
                IEnrichmentItem enrichment = category.Key;
                HashSet<XNode> enrichedNodes = category.Value;

                var outputData = new Dictionary<CartesianPoint, double>();
                foreach (XNode node in enrichedNodes)
                {
                    //TODO: This does not work if 1 enrichment item uses multiple enrichment functions, e.g. crack tip
                    double[] enrichedValues = node.EnrichmentItems[enrichment];
                    outputData[node] = enrichedValues[0]; 
                }

                string suffix = (geometricModel.SingleCurves.Count == 1) ? "" : $"{enrichmentID}";
                ++enrichmentID;
                string file = $"{outputDirectory}\\enriched_nodes{suffix}.vtk";
                using (var writer = new VtkPointWriter(file))
                {
                    writer.WriteScalarField("enriched_nodes", outputData);
                }
            }
        }

        private Dictionary<IEnrichmentItem, HashSet<XNode>> ClassifyEnrichedNodes()
        {
            var classifiedNodes = new Dictionary<IEnrichmentItem, HashSet<XNode>>();
            foreach (XNode node in model.Nodes)
            {
                foreach (IEnrichmentItem enrichment in node.EnrichmentItems.Keys)
                {
                    bool isEnrichmentStored = classifiedNodes.TryGetValue(enrichment, out HashSet<XNode> enrichedNodes);
                    if (!isEnrichmentStored)
                    {
                        enrichedNodes = new HashSet<XNode>();
                        classifiedNodes[enrichment] = enrichedNodes;
                    }
                    enrichedNodes.Add(node);
                }
            }
            return classifiedNodes;
        }
    }
}
