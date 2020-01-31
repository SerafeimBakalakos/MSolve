using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM.Multiphase.Enrichment
{
    public class Enricher
    {
        private readonly GeometricModel geometricModel;

        public Enricher(GeometricModel geometricModel)
        {
            this.geometricModel = geometricModel;
        }

        //TODO: Perhaps the boundary should store this data, instead of using a dictionary.
        public Dictionary<PhaseBoundary, StepEnrichment> StepEnrichments { get; }
            = new Dictionary<PhaseBoundary, StepEnrichment>();

        public void DefineEnrichments()
        {
            // Keep track of identified interactions between phases, to avoid duplicate enrichments
            var neighboringPhases = new Dictionary<IPhase, HashSet<IPhase>>();
            #region default phase
            //foreach (IPhase phase in geometricModel.Phases)
            #endregion
            for (int p = 1; p < geometricModel.Phases.Count; ++p)
            {
                neighboringPhases[geometricModel.Phases[p]] = new HashSet<IPhase>();
            }

            int id = 0;
            #region default phase
            //foreach (IPhase phase in geometricModel.Phases)
            #endregion
            for (int p = 1; p < geometricModel.Phases.Count; ++p)
            {
                var phase = (ConvexPhase)(geometricModel.Phases[p]);
                foreach (PhaseBoundary boundary in phase.Boundaries)
                {
                    IPhase otherPhase = (boundary.PositivePhase == this) ? boundary.NegativePhase : boundary.PositivePhase;
                    // This interaction may have been found for another boundary or the other phase
                    if (!neighboringPhases.ContainsKey(otherPhase)) 
                    {
                        neighboringPhases[phase].Add(otherPhase);
                        neighboringPhases[otherPhase].Add(phase);
                        StepEnrichments[boundary] = new StepEnrichment(id++, phase, otherPhase);
                    }
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
                        StepEnrichment enrichment = StepEnrichments[boundary];
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
