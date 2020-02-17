using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Enrichment.SingularityResolution;
using ISAAR.MSolve.XFEM.Multiphase.Entities;

//TODO: Add heaviside singularity resolver
//TODO: Remove casts
namespace ISAAR.MSolve.XFEM.Multiphase.Enrichment
{
    public class NodeEnricher2Junctions
    {
        private readonly GeometricModel geometricModel;
        private readonly ISingularityResolver singularityResolver;

        public NodeEnricher2Junctions(GeometricModel geometricModel, ISingularityResolver singularityResolver)
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
            #region default phase
            //foreach (IPhase phase in geometricModel.Phases)
            #endregion
            for (int p = 1; p < geometricModel.Phases.Count; ++p)
            {
                var phase = (ConvexPhase)(geometricModel.Phases[p]);
                foreach (IXFiniteElement element in phase.IntersectedElements)
                {
                    //TODO: Shouldn't the boundaries intersect?
                    if (element.Phases.Count <= 2) continue; // Not a junction element
                    else if (element.PhaseIntersections.Count <= 2) continue; // 3 or more phases, but the boundaries do not intersect
                    PhaseBoundary[] boundaries = element.PhaseIntersections.Keys.ToArray();

                    // If there are n boundaries intersecting, then use n-1 junctions
                    //TODO: Perhaps this is too simplistic. What happens if there are 3 phases, but their 2 boundaries do not 
                    //      intersect? Should I use 2 step enrichments instead? Also what happens if in an element 3 boundaries
                    //      intersect and 4th does not intersect?
                    for (int i = 0; i < boundaries.Length - 1; ++i) 
                    {
                        PhaseBoundary boundary = boundaries[i];

                        bool enrichmentExists = junctionEnrichments.TryGetValue(boundary, out JunctionEnrichment junction);
                        if (!enrichmentExists)
                        {
                            ++id;
                            junction = new JunctionEnrichment(id, boundary, element.Phases);
                            junctionEnrichments[boundary] = junction;
                        }

                        bool elementExists = JunctionElements.TryGetValue(element, 
                            out HashSet<JunctionEnrichment> elementJunctions);
                        if (!elementExists)
                        {
                            elementJunctions = new HashSet<JunctionEnrichment>();
                            JunctionElements[element] = elementJunctions;
                        }
                        elementJunctions.Add(junction);
                    }
                }
            }
            return id - idStart;
        }

        private int DefineStepEnrichments(int idStart)
        {
            // Keep track of identified interactions between phases, to avoid duplicate enrichments
            var uniqueEnrichments = new Dictionary<int, Dictionary<int, StepEnrichment>>();

            //TODO: This would be faster, but only works for consecutive phase IDs starting from 0.
            //var uniqueEnrichments = new Dictionary<int, StepEnrichment>[geometricModel.Phases.Count];

            #region default phase
            //foreach (IPhase phase in geometricModel.Phases)
            #endregion
            for (int p = 1; p < geometricModel.Phases.Count; ++p)
            {
                uniqueEnrichments[geometricModel.Phases[p].ID] = new Dictionary<int, StepEnrichment>();
            }

            int id = idStart;
            #region default phase
            //foreach (IPhase phase in geometricModel.Phases)
            #endregion
            for (int p = 1; p < geometricModel.Phases.Count; ++p)
            {
                foreach (PhaseBoundary boundary in geometricModel.Phases[p].Boundaries)
                {
                    // It may have been processed when iterating the boundaries of the opposite phase.
                    if (boundary.StepEnrichment != null) continue; 

                    // Find min/max phase IDs to uniquely identify the interaction
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
                        uniqueEnrichments[maxPhase.ID].TryGetValue(minPhase.ID, out StepEnrichment enrichment);
                    if (!enrichmentsExists)
                    {
                        enrichment = new StepEnrichment(id++, boundary.PositivePhase, boundary.NegativePhase);
                        uniqueEnrichments[maxPhase.ID][minPhase.ID] = enrichment;
                    }

                    boundary.StepEnrichment = enrichment;
                }
            }
            return id - idStart;
        }

        private void EnrichNode(XNode node, IEnrichment enrichment)
        {
            if (!node.Enrichments.ContainsKey(enrichment))
            {
                double value = enrichment.EvaluateAt(node);
                node.Enrichments[enrichment] = value;
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
            var nodesPerStepEnrichment = new Dictionary<IEnrichment, HashSet<XNode>>();
            #region default phase
            //foreach (IPhase phase in geometricModel.Phases)
            #endregion
            for (int p = 1; p < geometricModel.Phases.Count; ++p)
            {
                var phase = (ConvexPhase)(geometricModel.Phases[p]);
                foreach (IXFiniteElement element in phase.IntersectedElements)
                {
                    foreach (PhaseBoundary boundary in element.PhaseIntersections.Keys)
                    {
                        // Find the nodes to potentially be enriched by this step enrichment 
                        IEnrichment stepEnrichment = boundary.StepEnrichment;
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
                IEnrichment stepEnrichment = enrichmentNodesPair.Key;
                HashSet<XNode> nodesToEnrich = enrichmentNodesPair.Value;

                // Some of these nodes may need to not be enriched after all, to avoid singularities in the global stiffness matrix
                //HashSet<XNode> rejectedNodes = singularityResolver.FindStepEnrichedNodesToRemove(nodesToEnrich, stepEnrichment);

                // Enrich the rest of them
                //nodesToEnrich.ExceptWith(rejectedNodes);
                foreach (XNode node in nodesToEnrich) EnrichNode(node, stepEnrichment);
            }
        }

        private bool HasCorrespondingJunction(XNode node, IEnrichment stepEnrichment)
        {
            foreach (IEnrichment enrichment in node.Enrichments.Keys)
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
