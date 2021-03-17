using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment.Functions;
using MGroup.XFEM.Enrichment.SingularityResolution;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Phases;

//TODO: Determine whether to use step or ridge enrichments based on if each interface is cohesive or coherent.
//TODO: Resolving singularities is only needed in step enrichment (cohesive interfaces).
namespace MGroup.XFEM.Enrichment.Enrichers
{
    public class NodeEnricherMultiphaseNoJunctions : INodeEnricher
    {
        private readonly PhaseGeometryModel geometricModel;
        private readonly ISingularityResolver singularityResolver;
        private static readonly IDofType[] stdDofs = new IDofType[] { ThermalDof.Temperature };

        private Dictionary<EnrichmentItem, IPhaseBoundary> enrichments;

        public NodeEnricherMultiphaseNoJunctions(PhaseGeometryModel geometricModel)
            : this(geometricModel, new NullSingularityResolver())
        {
        }

        public NodeEnricherMultiphaseNoJunctions(PhaseGeometryModel geometricModel, 
            ISingularityResolver singularityResolver)
        {
            this.geometricModel = geometricModel;
            this.singularityResolver = singularityResolver;
        }


        public void ApplyEnrichments()
        {
            // Find nodes of elements interacting with each discontinuity. These nodes will potentially be enriched.
            var nodesPerEnrichment = new Dictionary<EnrichmentItem, HashSet<XNode>>();
            foreach (IPhase phase in geometricModel.Phases.Values)
            {
                if (phase is DefaultPhase) continue;
                foreach (IXMultiphaseElement element in phase.BoundaryElements)
                {
                    foreach (IPhaseBoundary boundary in element.PhaseIntersections.Keys)
                    {
                        EnrichmentItem enrichment = boundary.StepEnrichment;
                        bool exists = nodesPerEnrichment.TryGetValue(enrichment, out HashSet<XNode> nodesToEnrich);
                        if (!exists)
                        {
                            nodesToEnrich = new HashSet<XNode>();
                            nodesPerEnrichment[enrichment] = nodesToEnrich;
                        }
                        foreach (XNode node in element.Nodes) nodesToEnrich.Add(node);
                    }
                }
            }

            // Enrich these nodes with the corresponding enrichment
            foreach (var enrichmentNodesPair in nodesPerEnrichment)
            {
                EnrichmentItem enrichment = enrichmentNodesPair.Key;
                HashSet<XNode> nodesToEnrich = enrichmentNodesPair.Value;
                IPhaseBoundary boundary = this.enrichments[enrichment];

                // Some of these nodes may need to not be enriched after all, to avoid singularities in the global stiffness matrix
                HashSet<XNode> rejectedNodes = 
                    singularityResolver.FindStepEnrichedNodesToRemove(nodesToEnrich, boundary.Geometry);

                // Enrich the rest of them
                nodesToEnrich.ExceptWith(rejectedNodes);
                foreach (XNode node in nodesToEnrich)
                {
                    EnrichNode(node, enrichment);
                }
            }
        }

        public IEnumerable<EnrichmentItem> DefineEnrichments()
        {
            this.enrichments = new Dictionary<EnrichmentItem, IPhaseBoundary>();
            foreach (IPhase phase in geometricModel.Phases.Values)
            {
                if (phase is DefaultPhase) continue;
                if (phase.ExternalBoundaries.Count > 1)
                {
                    throw new NotImplementedException("This node enricher assumes that each phase has a single boundary");
                }
                IPhaseBoundary boundary = phase.ExternalBoundaries[0];

                var enrFunc = new PhaseStepEnrichment(boundary);
                var enrDofs = new IDofType[stdDofs.Length];
                for (int i = 0; i < stdDofs.Length; ++i) enrDofs[i] = new EnrichedDof(enrFunc, stdDofs[i]);
                var enrItem = new EnrichmentItem(this.enrichments.Count, new IEnrichmentFunction[] { enrFunc }, enrDofs);

                boundary.StepEnrichment = enrItem;
                this.enrichments[enrItem] = boundary;
            }

            return enrichments.Keys;
        }

        private void EnrichNode(XNode node, EnrichmentItem enrichment)
        {
            if (!node.Enrichments.Contains(enrichment))
            {
                node.Enrichments.Add(enrichment);
                foreach (IEnrichmentFunction enrichmentFunc in enrichment.EnrichmentFunctions)
                {
                    double value = enrichmentFunc.EvaluateAt(node);
                    node.EnrichmentFuncs[enrichmentFunc] = value;
                }
            }
        }
    }
}
