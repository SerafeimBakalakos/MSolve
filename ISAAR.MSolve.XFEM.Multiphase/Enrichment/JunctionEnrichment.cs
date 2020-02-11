using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM.Multiphase.Enrichment
{
    public class JunctionEnrichment : IEnrichment
    {
        private readonly HashSet<PhaseBoundary> boundaries;
        private readonly IPhase[] descendingPhases;

        //TODO: delete
        //public JunctionEnrichment(int id, IEnumerable<IPhase> phases)
        //{
        //    this.ID = id;
        //    this.descendingPhases = phases.ToArray();
        //    Array.Sort(descendingPhases, new PhaseComparer());
        //    this.Dof = new EnrichedDof(this, ThermalDof.Temperature);
        //}

        public JunctionEnrichment(int id, IEnumerable<PhaseBoundary> phaseBoundaries)
        {
            this.ID = id;
            this.boundaries = new HashSet<PhaseBoundary>(phaseBoundaries);

            var phases = new HashSet<IPhase>();
            foreach (PhaseBoundary boundary in phaseBoundaries)
            {
                phases.Add(boundary.PositivePhase);
                phases.Add(boundary.NegativePhase);
            }

            this.descendingPhases = phases.ToArray();
            Array.Sort(descendingPhases, new PhaseComparer());

            this.Dof = new EnrichedDof(this, ThermalDof.Temperature);
        }

        public EnrichedDof Dof { get; }

        public int ID { get; }

        public IReadOnlyList<IPhase> Phases => descendingPhases;

        public double EvaluateAt(XNode node) => FindPhaseAt(node).ID;

        public double EvaluateAt(CartesianPoint point)
        {
            // It is more efficient to avoid searching the default phase. This is why it is placed last (if present) as the else case.
            int lastPhase = descendingPhases.Length - 1;
            for (int p = 0; p < lastPhase; ++p)
            {
                IPhase phase = descendingPhases[p];
                if (phase.Contains(point)) return phase.ID;
            }
            return descendingPhases[lastPhase].ID;
        }

        public double EvaluateAt(IPhase phaseAtPoint) => phaseAtPoint.ID;

        public IPhase FindPhaseAt(XNode node)
        {
            return node.SurroundingPhase;
            //// It is more efficient to avoid searching the default phase. This is why it is placed last (if present) as the else case.
            //int lastPhase = descendingPhases.Length - 1;
            //for (int p = 0; p < lastPhase; ++p)
            //{
            //    IPhase phase = descendingPhases[p];
            //    if (phase.ContainedNodes.Contains(node)) return phase;
            //}

            ////TODO: Perhaps this should be checked in release configs as well
            //Debug.Assert(descendingPhases[lastPhase].ContainedNodes.Contains(node));
            //return descendingPhases[lastPhase];
        }

        public double GetJumpCoefficientBetween(PhaseBoundary phaseBoundary)
        {
            throw new NotImplementedException();
        }

        public bool IsAppliedDueTo(PhaseBoundary phaseBoundary) => boundaries.Contains(phaseBoundary);

        private class PhaseComparer : IComparer<IPhase>
        {
            public int Compare(IPhase x, IPhase y) => y.ID - x.ID; // Descending order
        }
    }
}
