﻿using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Enrichment.SingularityResolution;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Enrichment
{
    public class NodeEnricherOLD
    {
        private readonly GeometricModel geometricModel;
        private readonly ISingularityResolver singularityResolver;

        public NodeEnricherOLD(GeometricModel geometricModel, ISingularityResolver singularityResolver)
        {
            this.geometricModel = geometricModel;
            this.singularityResolver = singularityResolver;
            this.JunctionElements = new Dictionary<IXFiniteElement, JunctionEnrichmentOLD>();
        }

        public Dictionary<IXFiniteElement, JunctionEnrichmentOLD> JunctionElements { get; }

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
            //TODO: Perhaps the comparison should be done only when duplicate junctions are identified at the same node. 
            //      This would avoid a ton of comparisons.
            //TODO: This comparison results in using the same enrichment for multiple junction points, that are all between the 
            //      same 3 (or more) phases, but at different coordinates. Is this a good thing or not?
            var comparer = new JunctionComparer();
            var junctionEnrichments = new SortedDictionary<JunctionEnrichmentOLD, JunctionEnrichmentOLD>(comparer);

            int id = idStart;
            #region default phase
            //foreach (IPhase phase in geometricModel.Phases)
            #endregion
            for (int p = 1; p < geometricModel.Phases.Count; ++p)
            {
                var phase = (ConvexPhase)(geometricModel.Phases[p]);
                foreach (IXFiniteElement element in phase.IntersectedElements)
                {
                    if (element.Phases.Count <= 2) continue; // Not a junction element

                    var newJunction = new JunctionEnrichmentOLD(id, element.PhaseIntersections.Keys);
                    bool alreadyExists = junctionEnrichments.TryGetValue(newJunction, out JunctionEnrichmentOLD oldJunction);
                    if (!alreadyExists)
                    {
                        ++id;
                        junctionEnrichments[newJunction] = newJunction;
                        JunctionElements[element] = newJunction;
                    }
                    else JunctionElements[element] = oldJunction;
                }
            }
            return id - idStart;
        }

        private int DefineStepEnrichments(int idStart)
        {
            // Keep track of identified interactions between phases, to avoid duplicate enrichments
            var uniqueEnrichments = new Dictionary<int, Dictionary<int, StepEnrichmentOLD>>();

            //TODO: This would be faster, but only works for consecutive phase IDs starting from 0.
            //var uniqueEnrichments = new Dictionary<int, StepEnrichment>[geometricModel.Phases.Count];

            #region default phase
            //foreach (IPhase phase in geometricModel.Phases)
            #endregion
            for (int p = 1; p < geometricModel.Phases.Count; ++p)
            {
                uniqueEnrichments[geometricModel.Phases[p].ID] = new Dictionary<int, StepEnrichmentOLD>();
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
                        uniqueEnrichments[maxPhase.ID].TryGetValue(minPhase.ID, out StepEnrichmentOLD enrichment);
                    if (!enrichmentsExists)
                    {
                        enrichment = new StepEnrichmentOLD(id++, boundary.PositivePhase, boundary.NegativePhase);
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
                JunctionEnrichmentOLD junctionEnrichment = elementJunctionPair.Value;
                foreach (XNode node in element.Nodes) EnrichNode(node, junctionEnrichment);
            }

            // Find nodes to potentially be enriched by step enrichments
            var nodesPerStepEnrichment = new Dictionary<StepEnrichmentOLD, HashSet<XNode>>();
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
                        StepEnrichmentOLD stepEnrichment = (StepEnrichmentOLD)boundary.StepEnrichment;
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
                StepEnrichmentOLD stepEnrichment = enrichmentNodesPair.Key;
                HashSet<XNode> nodesToEnrich = enrichmentNodesPair.Value;

                // Some of these nodes may need to not be enriched after all, to avoid singularities in the global stiffness matrix
                HashSet<XNode> rejectedNodes = singularityResolver.FindStepEnrichedNodesToRemove(nodesToEnrich, stepEnrichment);

                // Enrich the rest of them
                nodesToEnrich.ExceptWith(rejectedNodes);
                foreach (XNode node in nodesToEnrich) EnrichNode(node, stepEnrichment);
            }
            
        }
        

        private bool HasCorrespondingJunction(XNode node, StepEnrichmentOLD stepEnrichment)
        {
            foreach (IEnrichment enrichment in node.Enrichments.Keys)
            {
                if (enrichment is JunctionEnrichmentOLD junctionEnrichment)
                {
                    // Also make sure the junction and step enrichment refer to the same phases.
                    var junctionPhases = new HashSet<IPhase>(junctionEnrichment.Phases);
                    if (junctionPhases.IsSupersetOf(stepEnrichment.Phases)) return true;
                }
            }
            return false;
        }

        private class JunctionComparer : IComparer<JunctionEnrichmentOLD>
        {
            public int Compare(JunctionEnrichmentOLD x, JunctionEnrichmentOLD y)
            {
                int result = x.Phases.Count - y.Phases.Count;
                if (result != 0) return result; // Junctions with fewer elements go first
                
                //TODO: Guarantee that the phases are ordered in descending order
                for (int p = 0; p < x.Phases.Count; ++p)
                {
                    // As long as the i-th phase is the same, continue comparing.
                    result = x.Phases[p].ID - y.Phases[p].ID;
                    if (result != 0) return result; 
                }

                return 0; // At this point all phases are the same.
            }
        }
    }
}
