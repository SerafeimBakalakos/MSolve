using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Enrichment.SingularityResolution;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Entities;

//TODO: Add heaviside singularity resolver
//TODO: Remove casts
namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Enrichment
{
    public class NodeEnricher_v2
    {
        private readonly GeometricModel geometricModel;
        private readonly XModel physicalModel;
        private readonly ISingularityResolver singularityResolver;

        public NodeEnricher_v2(XModel physicalModel, GeometricModel geometricModel, ISingularityResolver singularityResolver)
        {
            this.physicalModel = physicalModel;
            this.geometricModel = geometricModel;
            this.singularityResolver = singularityResolver;
            this.JunctionElements = new Dictionary<IXFiniteElement, HashSet<IJunctionEnrichment>>();
        }

        public Dictionary<IXFiniteElement, HashSet<IJunctionEnrichment>> JunctionElements { get; }

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
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                // Check if the element contains a junction point: > 2 phases
                if (element.Phases.Count <= 2) continue;

                // It is possible that there are > 2 phases, but their boundaries do not intersect
                else if (!VerifyJunction(element)) continue;

                if (element.Phases.Count > 3) throw new NotImplementedException();

                // Based on these interactions, we will apply junction enrichments
                List<(IPhase phase0, IPhase phase1)> phaseInteractions = FindPhaseInteractions(element);
                var elementJunctions = new HashSet<IJunctionEnrichment>();
                JunctionElements[element] = elementJunctions;

                // If there are common nodes with another junction element and the 2 junctions involve the same phases,
                // then adding similar junction will lead to singularities. Identify the common phase interactions.
                HashSet<IXFiniteElement> junctionNeighbors = FindElementJunctionNeighbors(element);
                foreach (IXFiniteElement otherElement in junctionNeighbors)
                {
                    List<(IPhase phase0, IPhase phase1)> phaseInteractionsOther = FindPhaseInteractions(otherElement);
                    bool overlapped = false;
                    (phaseInteractions, overlapped) = FindDistinctPhaseInteractions(phaseInteractions, phaseInteractionsOther);
                    if (overlapped)
                    {
                        //Debug.Assert(JunctionElements[otherElement].Count == 1);
                        elementJunctions.UnionWith(JunctionElements[otherElement]);
                    }
                }

                // For all interactions except the last, apply a junction enrichment
                for (int i = 0; i < phaseInteractions.Count - 1; ++i) // n-1 enrichments for n boundaries
                {
                    var junction = new JunctionEnrichment_v2(id, phaseInteractions[i].phase0, phaseInteractions[i].phase1);
                    ++id;
                    elementJunctions.Add(junction);
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
                foreach (IJunctionEnrichment junctionEnrichment in elementJunctionPair.Value)
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

        /// <summary>
        /// Find the subset of <paramref name="interactions"/> that does not exist in <paramref name="otherInteractions"/>.
        /// </summary>
        /// <param name="interactions"></param>
        /// <param name="otherInteractions"></param>
        private (List<(IPhase, IPhase)>, bool overlapped) FindDistinctPhaseInteractions(
            List<(IPhase, IPhase)> interactions, List<(IPhase, IPhase)> otherInteractions)
        {
            bool overlapped = false;
            var distinctInteractions = new List<(IPhase, IPhase)>();
            foreach ((IPhase p0, IPhase p1) in interactions)
            {
                bool interactionExists = false;
                foreach ((IPhase p2, IPhase p3) in otherInteractions)
                {
                    bool sameInteraction = false;
                    sameInteraction |= (p0.ID == p2.ID) && (p1.ID == p3.ID);
                    sameInteraction |= (p1.ID == p2.ID) && (p0.ID == p3.ID);

                    if (!sameInteraction)
                    {
                        interactionExists = true;
                        break;
                    }
                }
                if (!interactionExists) distinctInteractions.Add((p0, p1));
                else overlapped = true;
            }
            return (distinctInteractions, overlapped);
        }

        private HashSet<IXFiniteElement> FindElementJunctionNeighbors(IXFiniteElement element)
        {
            var neighbors = new HashSet<IXFiniteElement>();
            foreach (XNode node in element.Nodes)
            {
                neighbors.UnionWith(node.ElementsDictionary.Values);
            }
            neighbors.Remove(element);

            var junctionNeighbors = new HashSet<IXFiniteElement>();
            foreach (IXFiniteElement neighbor in neighbors)
            {
                if (JunctionElements.ContainsKey(neighbor)) junctionNeighbors.Add(neighbor);
            }
            return junctionNeighbors;
        }

        private List<(IPhase phase0, IPhase phase1)> FindPhaseInteractions(IXFiniteElement element)
        {
            var remainderPhases = new HashSet<IPhase>(element.Phases);
            var phaseInteractions = new List<(IPhase phase0, IPhase phase1)>();
            foreach (IPhase phase in element.Phases)
            {
                remainderPhases.Remove(phase);
                foreach (IPhase otherPhase in remainderPhases)
                {
                    // Make sure there is a common boundary
                    bool areNeighbors = false;
                    foreach (PhaseBoundary boundary in element.PhaseIntersections.Keys)
                    {
                        if ((boundary.PositivePhase.ID == phase.ID && boundary.NegativePhase.ID == otherPhase.ID)
                            || (boundary.NegativePhase.ID == phase.ID && boundary.PositivePhase.ID == otherPhase.ID))
                        {
                            areNeighbors = true;
                            break;
                        }
                    }
                    if (areNeighbors) phaseInteractions.Add((phase, otherPhase));
                }
            }
            return phaseInteractions;
        }

        private bool HasCorrespondingJunction(XNode node, IEnrichment stepEnrichment)
        {
            foreach (IEnrichment enrichment in node.Enrichments.Keys)
            {
                if (enrichment is JunctionEnrichment_v2 junctionEnrichment)
                {
                    // Also make sure the junction and step enrichment refer to the same phases.
                    Debug.Assert(stepEnrichment.Phases.Count == 2); // Perhaps IEnrichment.Phases should be removed.
                    if (junctionEnrichment.IntroducesJumpBetween(stepEnrichment.Phases[0], stepEnrichment.Phases[1])) return true;
                }
            }
            return false;
        }

        private bool VerifyJunction(IXFiniteElement element)
        {
            var uniquePhaseSeparators = new Dictionary<int, HashSet<int>>();
            foreach (PhaseBoundary boundary in element.PhaseIntersections.Keys)
            {
                (IPhase minPhase, IPhase maxPhase) = FindMinMaxPhases(boundary.PositivePhase, boundary.NegativePhase);
                bool exists = uniquePhaseSeparators.TryGetValue(minPhase.ID, out HashSet<int> neighbors);
                if (!exists)
                {
                    neighbors = new HashSet<int>();
                    uniquePhaseSeparators[minPhase.ID] = neighbors;
                }
                neighbors.Add(maxPhase.ID);
            }

            int numUniqueSeparators = 0;
            foreach (HashSet<int> neighbors in uniquePhaseSeparators.Values) numUniqueSeparators += neighbors.Count;

            if (numUniqueSeparators <= 2) return false; // 3 or more phases, but the boundaries do not intersect
            return true;
        }
    }
}
