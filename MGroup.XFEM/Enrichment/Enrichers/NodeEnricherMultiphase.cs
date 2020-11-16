using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment.Functions;
using MGroup.XFEM.Enrichment.SingularityResolution;
using MGroup.XFEM.Entities;

//TODO: Remove casts
namespace MGroup.XFEM.Enrichment.Enrichers
{
    public class NodeEnricherMultiphase : INodeEnricher
    {
        private readonly PhaseGeometryModel geometricModel;
        private readonly ISingularityResolver singularityResolver;

        public NodeEnricherMultiphase(PhaseGeometryModel geometricModel, ISingularityResolver singularityResolver)
        {
            this.geometricModel = geometricModel;
            this.singularityResolver = singularityResolver;
            this.JunctionElements = new Dictionary<IXFiniteElement, HashSet<JunctionEnrichment>>();
        }

        public Dictionary<IXFiniteElement, HashSet<JunctionEnrichment>> JunctionElements { get; }

        public void ApplyEnrichments()
        {
            int numStepEnrichments = DefineStepEnrichments(0);
            int numJunctionEnrichments = DefineJunctionEnrichments(numStepEnrichments);
            EnrichNodes();
        }

        private static (IPhase minPhase, IPhase maxPhase) FindMinMaxPhases(IPhase phase1, IPhase phase2)
        {
            IPhase minPhase, maxPhase;
            if (phase1.ID < phase2.ID)
            {
                minPhase = phase1;
                maxPhase = phase2;
            }
            else
            {
                minPhase = phase2;
                maxPhase = phase1;
            }
            return (minPhase, maxPhase);
        }

        /// <summary>
        /// Assumes 0 or 1 junction per element
        /// </summary>
        /// <param name="idStart"></param>
        private int DefineJunctionEnrichments(int idStart) 
        {
            // Keep track of the junctions to avoid duplicate ones.
            //TODO: What happens if the same boundary is used for more than one junctions? E.g. the boundary is an almost closed 
            //      curve, that has 2 ends, both of which need junctions.
            var junctionEnrichments = new Dictionary<PhaseBoundary, JunctionEnrichment>();

            int id = idStart;
            //for (int p = 1; p < geometricModel.Phases.Count; ++p)
            //{
            //    IPhase phase = geometricModel.Phases[p];
            //    foreach (IXFiniteElement element in phase.BoundaryElements)
            //    {
            //        // This element has already been processed when looking at another phase
            //        if (JunctionElements.ContainsKey(element)) continue;

            //        // Check if the element contains a junction point
            //        //TODO: Shouldn't the boundaries intersect?
            //        if (element.Phases.Count <= 2) continue; // Not a junction element
            //        else
            //        {
            //            var uniquePhaseSeparators = new Dictionary<int, HashSet<int>>();
            //            foreach (PhaseBoundary boundary in element.PhaseIntersections.Keys)
            //            {
            //                (IPhase minPhase, IPhase maxPhase) = FindMinMaxPhases(boundary.PositivePhase, boundary.NegativePhase);
            //                bool exists = uniquePhaseSeparators.TryGetValue(minPhase.ID, out HashSet<int> neighbors);
            //                if (!exists)
            //                {
            //                    neighbors = new HashSet<int>();
            //                    uniquePhaseSeparators[minPhase.ID] = neighbors;
            //                }
            //                neighbors.Add(maxPhase.ID);
            //            }

            //            int numUniqueSeparators = 0;
            //            foreach (HashSet<int> neighbors in uniquePhaseSeparators.Values) numUniqueSeparators += neighbors.Count;

            //            if (numUniqueSeparators <= 2) continue; // 3 or more phases, but the boundaries do not intersect
            //        }

            //        // Create a new junction enrichment
            //        // If there are n boundaries intersecting, then use n-1 junctions
            //        PhaseBoundary[] boundaries = element.PhaseIntersections.Keys.ToArray();
            //        var elementJunctions = new HashSet<JunctionEnrichment>();
            //        JunctionElements[element] = elementJunctions;
            //        for (int i = 0; i < boundaries.Length - 1; ++i) 
            //        {
            //            PhaseBoundary boundary = boundaries[i];
            //            var junction = new JunctionEnrichment(id, boundary, element.Phases);
            //            ++id;
            //            elementJunctions.Add(junction);
            //        }
            //    }
            //}
            return id - idStart;
        }

        private int DefineStepEnrichments(int idStart)
        {
            // Keep track of identified interactions between phases, to avoid duplicate enrichments
            var uniqueEnrichments = new Dictionary<int, Dictionary<int, PhaseStepEnrichment>>();

            //TODO: This would be faster, but only works for consecutive phase IDs starting from 0.
            //var uniqueEnrichments = new Dictionary<int, StepEnrichment>[geometricModel.Phases.Count];

            for (int p = 1; p < geometricModel.Phases.Count; ++p)
            {
                uniqueEnrichments[geometricModel.Phases[p].ID] = new Dictionary<int, PhaseStepEnrichment>();
            }

            int id = idStart;
            for (int p = 1; p < geometricModel.Phases.Count; ++p)
            {
                foreach (PhaseBoundary boundary in geometricModel.Phases[p].ExternalBoundaries)
                {
                    // It may have been processed when iterating the boundaries of the opposite phase.
                    if (boundary.StepEnrichment != null) continue;

                    // Find min/max phase IDs to uniquely identify the interaction.
                    //TODO: Why not uniquely identify the interaction based on the boundary object?
                    IPhase minPhase, maxPhase;
                    if (boundary.PositivePhase.ID < boundary.NegativePhase.ID)
                    {
                        minPhase = boundary.PositivePhase;
                        maxPhase = boundary.NegativePhase;
                    }
                    else
                    {
                        minPhase = boundary.NegativePhase;
                        maxPhase = boundary.PositivePhase;
                    }

                    // Find the existing enrichment for this phase interaction or create a new one
                    bool enrichmentsExists = 
                        uniqueEnrichments[maxPhase.ID].TryGetValue(minPhase.ID, out PhaseStepEnrichment enrichment);
                    if (!enrichmentsExists)
                    {
                        enrichment = DefineStepEnrichment(id, boundary);
                        ++id;
                        uniqueEnrichments[maxPhase.ID][minPhase.ID] = enrichment;
                    }

                    boundary.StepEnrichment = enrichment;
                }
            }
            return id - idStart;
        }

        private PhaseStepEnrichment DefineStepEnrichment(int id, PhaseBoundary boundary)
        {
            if (boundary.NegativePhase is DefaultPhase)
            {
                return new PhaseStepEnrichment(id, boundary.PositivePhase, boundary.NegativePhase);
            }
            else if (boundary.PositivePhase is DefaultPhase)
            {
                return new PhaseStepEnrichment(id, boundary.NegativePhase, boundary.PositivePhase);
            }
            else if (/*boundary.PositivePhase is HollowPhase &&*/ boundary.NegativePhase is ConvexPhase)
            {
                return new PhaseStepEnrichment(id, boundary.NegativePhase, boundary.PositivePhase);
            }
            else if (/*boundary.NegativePhase is HollowPhase &&*/ boundary.PositivePhase is ConvexPhase)
            {
                return new PhaseStepEnrichment(id, boundary.PositivePhase, boundary.NegativePhase);
            }
            else // Does not matter which phase will be internal/external
            {
                return new PhaseStepEnrichment(id, boundary.NegativePhase, boundary.PositivePhase);
            }
        }

        private void EnrichNode(XNode node, IEnrichmentFunction enrichment)
        {
            if (!node.EnrichmentFuncs.ContainsKey(enrichment))
            {
                double value = enrichment.EvaluateAt(node);
                node.EnrichmentFuncs[enrichment] = value;
            }
        }

        private void EnrichNodes()
        {
            // Junction enrichments
            foreach (var elementJunctionPair in JunctionElements)
            {
                IXFiniteElement element = elementJunctionPair.Key;
                foreach (JunctionEnrichment junctionEnrichment in elementJunctionPair.Value)
                {
                    foreach (XNode node in element.Nodes) EnrichNode(node, junctionEnrichment);
                }
            }

            // Find nodes to potentially be enriched by step enrichments
            var nodesPerStepEnrichment = new Dictionary<IEnrichmentFunction, HashSet<XNode>>();
            foreach (IPhase phase in geometricModel.Phases)
            {
                if (phase is DefaultPhase) continue;
                foreach (IXMultiphaseElement element in phase.BoundaryElements)
                {
                    foreach (PhaseBoundary boundary in element.PhaseIntersections.Keys)
                    {
                        // Find the nodes to potentially be enriched by this step enrichment 
                        IEnrichmentFunction stepEnrichment = boundary.StepEnrichment;
                        bool exists = nodesPerStepEnrichment.TryGetValue(stepEnrichment, out HashSet<XNode> nodesToEnrich);
                        if (!exists)
                        {
                            nodesToEnrich = new HashSet<XNode>();
                            nodesPerStepEnrichment[stepEnrichment] = nodesToEnrich;
                        }

                        // Only enrich a node if it does not have a corresponding junction enrichment
                        foreach (XNode node in element.Nodes)
                        {
                            if (!HasCorrespondingJunction(node, stepEnrichment)) nodesToEnrich.Add(node);
                        }
                    }
                }
            }

            // Enrich these nodes with the corresponding step enrichment
            foreach (var enrichmentNodesPair in nodesPerStepEnrichment)
            {
                IEnrichmentFunction stepEnrichment = enrichmentNodesPair.Key;
                HashSet<XNode> nodesToEnrich = enrichmentNodesPair.Value;

                // Some of these nodes may need to not be enriched after all, to avoid singularities in the global stiffness matrix
                //HashSet<XNode> rejectedNodes = singularityResolver.FindStepEnrichedNodesToRemove(nodesToEnrich, stepEnrichment);

                // Enrich the rest of them
                //nodesToEnrich.ExceptWith(rejectedNodes);
                foreach (XNode node in nodesToEnrich)
                {
                    EnrichNode(node, stepEnrichment);
                }
            }
        }

        private bool HasCorrespondingJunction(XNode node, IEnrichmentFunction stepEnrichment)
        {
            foreach (IEnrichmentFunction enrichment in node.EnrichmentFuncs.Keys)
            {
                if (enrichment is JunctionEnrichment junctionEnrichment)
                {
                    // Also make sure the junction and step enrichment refer to the same phases.
                    var junctionPhases = new HashSet<IPhase>(junctionEnrichment.Phases);
                    if (junctionPhases.IsSupersetOf(stepEnrichment.Phases)) return true;
                }
            }
            return false;
        }
    }
}
