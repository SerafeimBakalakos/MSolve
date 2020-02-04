using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Entities;

//TODO: Add heaviside singularity resolver
namespace ISAAR.MSolve.XFEM.Multiphase.Enrichment
{
    public class NodeEnricher
    {
        private readonly GeometricModel geometricModel;

        public NodeEnricher(GeometricModel geometricModel)
        {
            this.geometricModel = geometricModel;
            this.JunctionElements = new Dictionary<IXFiniteElement, JunctionEnrichment>();
        }

        public Dictionary<IXFiniteElement, JunctionEnrichment> JunctionElements { get; }

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
            var junctionEnrichments = new SortedDictionary<JunctionEnrichment, JunctionEnrichment>(comparer);

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

                    var newJunction = new JunctionEnrichment(id, element.Phases);
                    bool alreadyExists = junctionEnrichments.TryGetValue(newJunction, out JunctionEnrichment oldJunction);
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
                    if (boundary.Enrichment != null) continue; 

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
                        enrichment = new StepEnrichment(id++, minPhase, maxPhase);
                        uniqueEnrichments[maxPhase.ID][minPhase.ID] = enrichment;
                    }

                    boundary.Enrichment = enrichment;
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
                JunctionEnrichment junctionEnrichment = elementJunctionPair.Value;
                foreach (XNode node in element.Nodes) EnrichNode(node, junctionEnrichment);
            }

            // Step enrichments
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
                        StepEnrichment stepEnrichment = boundary.Enrichment;
                        foreach (XNode node in element.Nodes)
                        {
                            // Only enrich a node if it does not have a corresponding junction enrichment
                            if (!HasCorrespondingJunction(node, stepEnrichment)) EnrichNode(node, stepEnrichment);
                        }
                    }
                }
            }
        }
        

        private bool HasCorrespondingJunction(XNode node, StepEnrichment stepEnrichment)
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

        private class JunctionComparer : IComparer<JunctionEnrichment>
        {
            public int Compare(JunctionEnrichment x, JunctionEnrichment y)
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
