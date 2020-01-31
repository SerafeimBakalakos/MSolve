using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.XFEM.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM.Multiphase.Enrichment
{
    public class StepEnrichment : IEnrichment
    {
        private readonly IPhase phase0, phase1;

        public StepEnrichment(int id, IPhase phase0, IPhase phase1)
        {
            this.ID = id;
            this.phase0 = phase0;
            this.phase1 = phase1;
            this.Dof = new EnrichedDof(this, ThermalDof.Temperature);
        }

        public EnrichedDof Dof { get; }

        public int ID { get; }

        public double EvaluateAt(XNode node)
        {
            if (phase0.ContainedNodes.Contains(node)) return phase0.ID;
            else return phase1.ID;
        }

    }
}
