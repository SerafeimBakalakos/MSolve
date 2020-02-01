using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM.Multiphase.Enrichment
{
    public class NodeEnricher
    {
        private readonly GeometricModel geometricModel;

        public NodeEnricher(GeometricModel geometricModel)
        {
            this.geometricModel = geometricModel;
        }

        public void DefineStepEnrichments()
        {
            // Keep track of identified interactions between phases, to avoid duplicate enrichments
            var uniqueEnrichments = new Dictionary<int, StepEnrichment>[geometricModel.Phases.Count];
            #region default phase
            //foreach (IPhase phase in geometricModel.Phases)
            #endregion
            for (int p = 1; p < geometricModel.Phases.Count; ++p) uniqueEnrichments[p] = new Dictionary<int, StepEnrichment>();

            int id = 0;
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
        }

        public void EnrichNodes()
        {
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
                        StepEnrichment enrichment = boundary.Enrichment;
                        foreach (XNode node in element.Nodes)
                        {
                            if (!node.Enrichments.ContainsKey(enrichment))
                            {
                                double value = enrichment.EvaluateAt(node);
                                node.Enrichments[enrichment] = value;
                            }
                        }
                    }
                }
            }
        }
    }
}
