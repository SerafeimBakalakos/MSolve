using System;
using System.Collections.Generic;
using System.Linq;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Logging.VTK;
using ISAAR.MSolve.XFEM.Multiphase.Enrichment;
using ISAAR.MSolve.XFEM.Multiphase.Entities;

//TODO: If a node has 2 step enrichments, then only 1 will be displayed. Perhaps slightly offset the multiple concidental points.
namespace ISAAR.MSolve.XFEM.Multiphase.Plotting.Enrichments
{
    public class EnrichmentPlotter
    {
        private readonly double elementSize;
        private readonly XModel physicalModel;

        public EnrichmentPlotter(XModel model, double elementSize)
        {
            this.physicalModel = model;
            this.elementSize = elementSize;
        }

        public void PlotJunctionEnrichedNodes(string path)
        {
            PlotEnrichedNodesCategory(enr => enr is JunctionEnrichment, path, "junction_enriched_nodes");
        }

        public void PlotStepEnrichedNodes(string path)
        {
            PlotEnrichedNodesCategory(enr => enr is StepEnrichment, path, "step_enriched_nodes");
        }

        private void PlotEnrichedNodesCategory(Func<IEnrichment, bool> predicate, string path, string categoryName)
        {
            var nodesToPlot = new Dictionary<CartesianPoint, double>();
            foreach (XNode node in physicalModel.Nodes)
            {
                if (node.Enrichments.Count == 0) continue;
                IEnrichment[] enrichments = node.Enrichments.Keys.Where(predicate).ToArray();
                if (enrichments.Length == 1)
                {
                    var point = new CartesianPoint(node.X, node.Y, node.Z);
                    nodesToPlot[point] = enrichments[0].ID;
                }
                else
                {
                    var nodeInstances = DuplicateNodeForBetterViewing(node, enrichments.Length);
                    for (int e = 0; e < enrichments.Length; ++e)
                    {
                        CartesianPoint point = nodeInstances[e];
                        nodesToPlot[point] = enrichments[e].ID;
                    }
                }

            }
            using (var writer = new VtkPointWriter(path))
            {
                writer.WriteScalarField(categoryName, nodesToPlot);
            }
        }

        private CartesianPoint[] DuplicateNodeForBetterViewing(XNode node, int numInstances)
        {
            //TODO: Add more.
            var possibilites = new CartesianPoint[4]; // The further ones apart go to top
            double offset = 0.05 * elementSize;
            possibilites[0] = new CartesianPoint(node.X - offset, node.Y - offset);
            possibilites[1] = new CartesianPoint(node.X + offset, node.Y + offset);
            possibilites[2] = new CartesianPoint(node.X + offset, node.Y - offset);
            possibilites[3] = new CartesianPoint(node.X - offset, node.Y + offset);

            var instances = new CartesianPoint[numInstances];
            for (int i = 0; i < numInstances; ++i) instances[i] = possibilites[i];
            return instances;
        }
    }
}
