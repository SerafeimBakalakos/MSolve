using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.XFEM.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM.Multiphase.Enrichment
{
    public class JunctionEnrichment : IEnrichment
    {
        private readonly IPhase[] descendingPhases;
        public JunctionEnrichment(int id, IEnumerable<IPhase> phases)
        {
            this.ID = id;
            this.descendingPhases = phases.ToArray();
            Array.Sort(descendingPhases, new PhaseComparer());
            this.Dof = new EnrichedDof(this, ThermalDof.Temperature);
        }

        public EnrichedDof Dof { get; }

        public int ID { get; }

        public IReadOnlyList<IPhase> Phases => descendingPhases;

        public double EvaluateAt(XNode node)
        {
            // It is more efficient to avoid searching the default phase. This is why it is placed last (if present) as the else case.
            int lastPhase = descendingPhases.Length - 1;
            for (int p = 0; p < lastPhase; ++p)
            {
                IPhase phase = descendingPhases[p];
                if (phase.ContainedNodes.Contains(node)) return phase.ID;
            }

            //TODO: Perhaps this should be checked in release configs as well
            Debug.Assert(descendingPhases[lastPhase].ContainedNodes.Contains(node));
            return descendingPhases[lastPhase].ID;
        }

        private class PhaseComparer : IComparer<IPhase>
        {
            public int Compare(IPhase x, IPhase y) => y.ID - x.ID; // Descending order
        }
    }
}
