using System;
using System.Collections.Generic;
using System.Linq;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Logging.VTK;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Enrichment;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Plotting.Enrichments
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
            PlotEnrichedNodesCategory(
                enr => (enr is JunctionEnrichmentOLD) || (enr is DauxJunctionEnrichment) || (enr is IJunctionEnrichment), 
            path, "junction_enriched_nodes");
        }

        public void PlotStepEnrichedNodes(string path)
        {
            PlotEnrichedNodesCategory(
                enr => (enr is StepEnrichmentOLD) || (enr is DauxHeavisideEnrichment) || (enr is StepEnrichment), 
                path, "step_enriched_nodes");
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
            var possibilites = new CartesianPoint[20]; // The further ones apart go to top
            double offset = 0.05 * elementSize;
            possibilites[0] = new CartesianPoint(node.X - offset, node.Y - offset);
            possibilites[1] = new CartesianPoint(node.X + offset, node.Y + offset);
            possibilites[2] = new CartesianPoint(node.X + offset, node.Y - offset);
            possibilites[3] = new CartesianPoint(node.X - offset, node.Y + offset);

            possibilites[4] = new CartesianPoint(node.X - 2.0 * offset, node.Y - 2.0 * offset);
            possibilites[5] = new CartesianPoint(node.X + 2.0 * offset, node.Y + 2.0 * offset);
            possibilites[6] = new CartesianPoint(node.X + 2.0 * offset, node.Y - 2.0 * offset);
            possibilites[7] = new CartesianPoint(node.X - 2.0 * offset, node.Y + 2.0 * offset);

            possibilites[8] = new CartesianPoint(node.X - 3.0 * offset, node.Y - 3.0 * offset);
            possibilites[9] = new CartesianPoint(node.X + 3.0 * offset, node.Y + 3.0 * offset);
            possibilites[10] = new CartesianPoint(node.X + 3.0 * offset, node.Y - 3.0 * offset);
            possibilites[11] = new CartesianPoint(node.X - 3.0 * offset, node.Y + 3.0 * offset);

            possibilites[12] = new CartesianPoint(node.X - 4.0 * offset, node.Y - 4.0 * offset);
            possibilites[13] = new CartesianPoint(node.X + 4.0 * offset, node.Y + 4.0 * offset);
            possibilites[14] = new CartesianPoint(node.X + 4.0 * offset, node.Y - 4.0 * offset);
            possibilites[15] = new CartesianPoint(node.X - 4.0 * offset, node.Y + 4.0 * offset);

            possibilites[16] = new CartesianPoint(node.X - 5.0 * offset, node.Y - 5.0 * offset);
            possibilites[17] = new CartesianPoint(node.X + 5.0 * offset, node.Y + 5.0 * offset);
            possibilites[18] = new CartesianPoint(node.X + 5.0 * offset, node.Y - 5.0 * offset);
            possibilites[19] = new CartesianPoint(node.X - 5.0 * offset, node.Y + 5.0 * offset);

            var instances = new CartesianPoint[numInstances];
            for (int i = 0; i < numInstances; ++i) instances[i] = possibilites[i];
            return instances;
        }
    }
}
