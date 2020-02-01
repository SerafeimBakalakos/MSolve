using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.XFEM.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM.Multiphase.Enrichment
{
    public class StepEnrichment : IEnrichment
    {
        private readonly IPhase minPhase, maxPhase;

        public StepEnrichment(int id, IPhase phase0, IPhase phase1)
        {
            this.ID = id;
            if (phase0.ID < phase1.ID)
            {
                this.minPhase = phase0;
                this.maxPhase = phase1;
            }
            else
            {
                this.minPhase = phase1;
                this.maxPhase = phase0;
            }
            this.Dof = new EnrichedDof(this, ThermalDof.Temperature);
        }

        public EnrichedDof Dof { get; }

        public int ID { get; }

        public IReadOnlyList<IPhase> Phases => new IPhase[] { maxPhase, minPhase };


        public double EvaluateAt(XNode node)
        {
            // Looking in the phase with max ID is more efficient, since the default phase has id=0 and would be slower to search. 
            if (maxPhase.ContainedNodes.Contains(node)) return maxPhase.ID;
            else
            {
                //TODO: Perhaps this should be checked in release configs as well
                Debug.Assert(minPhase.ContainedNodes.Contains(node));
                return minPhase.ID; 
            }
        }

    }
}
