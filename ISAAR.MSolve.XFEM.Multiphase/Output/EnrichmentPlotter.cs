using System.Collections.Generic;
using System.Linq;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Logging.VTK;
using ISAAR.MSolve.XFEM.Multiphase.Enrichment;
using ISAAR.MSolve.XFEM.Multiphase.Entities;

//TODO: If a node has 2 step enrichments, then only 1 will be displayed. Perhaps slightly offset the multiple concidental points.
namespace ISAAR.MSolve.XFEM.Multiphase.Output.Enrichments
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

        public void PlotStepEnrichedNodes(string path)
        {
            var stepEnrichedNodes = new Dictionary<CartesianPoint, double>();
            foreach (XNode node in physicalModel.Nodes)
            {
                if (node.Enrichments.Count == 0) continue;
                IEnrichment[] stepEnrichments = node.Enrichments.Keys.Where(enr => enr is StepEnrichment).ToArray();
                if (stepEnrichments.Length == 1)
                {
                    var point = new CartesianPoint(node.X, node.Y, node.Z);
                    stepEnrichedNodes[point] = stepEnrichments[0].ID;
                }
                else
                {
                    var nodeInstances = DuplicateNodeForBetterViewing(node, stepEnrichments.Length);
                    for (int e = 0; e < stepEnrichments.Length; ++e)
                    {
                        CartesianPoint point = nodeInstances[e];
                        stepEnrichedNodes[point] = stepEnrichments[e].ID;
                    }
                }
                
            }
            using (var writer = new VtkPointWriter(path))
            {
                writer.WriteScalarField("step_enriched_nodes", stepEnrichedNodes);
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
