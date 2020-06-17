using System;
using System.Collections.Generic;
using System.Linq;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Logging.VTK;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Plotting.Writers
{
    public class EnrichmentPlotter
    {
        private readonly double elementSize;
        private readonly XModel physicalModel;
        private readonly bool plot3D;

        public EnrichmentPlotter(XModel model, double elementSize, bool plot3D)
        {
            this.physicalModel = model;
            this.elementSize = elementSize;
            this.plot3D = plot3D;
        }

        public void PlotJunctionEnrichedNodes(string path)
        {
            PlotEnrichedNodesCategory(
                enr => (enr is JunctionEnrichment), path, "junction_enriched_nodes");
        }

        public void PlotStepEnrichedNodes(string path)
        {
            PlotEnrichedNodesCategory(
                enr => (enr is StepEnrichment), path, "step_enriched_nodes");
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
                    CartesianPoint[] nodeInstances = DuplicateNodeForBetterViewing(node, enrichments.Length);
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

            CartesianPoint[] possibilites = new CartesianPoint[4]; // The further ones apart go to top
            if (!plot3D)
            {
                possibilites = new CartesianPoint[4]; // The further ones apart go to top
                double offset = 0.05 * elementSize;
                possibilites[0] = new CartesianPoint(node.X - offset, node.Y - offset);
                possibilites[1] = new CartesianPoint(node.X + offset, node.Y + offset);
                possibilites[2] = new CartesianPoint(node.X + offset, node.Y - offset);
                possibilites[3] = new CartesianPoint(node.X - offset, node.Y + offset);
            }
            else
            {
                possibilites = new CartesianPoint[8];
                double offset = 0.05 * elementSize;
                possibilites[0] = new CartesianPoint(node.X - offset, node.Y - offset, node.Z - offset);
                possibilites[1] = new CartesianPoint(node.X + offset, node.Y + offset, node.Z - offset);
                possibilites[2] = new CartesianPoint(node.X + offset, node.Y - offset, node.Z - offset);
                possibilites[3] = new CartesianPoint(node.X - offset, node.Y + offset, node.Z - offset);
                possibilites[4] = new CartesianPoint(node.X - offset, node.Y - offset, node.Z + offset);
                possibilites[5] = new CartesianPoint(node.X + offset, node.Y + offset, node.Z + offset);
                possibilites[6] = new CartesianPoint(node.X + offset, node.Y - offset, node.Z + offset);
                possibilites[7] = new CartesianPoint(node.X - offset, node.Y + offset, node.Z + offset);
            }

            var instances = new CartesianPoint[numInstances];
            for (int i = 0; i < numInstances; ++i) instances[i] = possibilites[i];
            return instances;
        }
    }
}
