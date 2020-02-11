using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM.Multiphase.Enrichment
{
    public class DauxJunctionEnrichment : IEnrichment
    {
        private readonly IPhase[] descendingPhases;
        private readonly int[] descendingPhaseCoeffs;

        public DauxJunctionEnrichment(int id, IPhase mainPhase, IPhase secondaryPhasePlus, IPhase secondaryPhaseMinus)
        {
            this.ID = id;

            this.descendingPhases = new IPhase[] { mainPhase, secondaryPhasePlus, secondaryPhaseMinus };
            this.descendingPhaseCoeffs = new int[] { 0, 1, -1 };
            Array.Sort(descendingPhases, descendingPhaseCoeffs, new PhaseComparer());

            this.Dof = new EnrichedDof(this, ThermalDof.Temperature);
        }

        public EnrichedDof Dof { get; }

        public int ID { get; }

        public IReadOnlyList<IPhase> Phases => descendingPhases;

        public double EvaluateAt(XNode node)
        {
            if (node.SurroundingPhase == descendingPhases[0]) return descendingPhaseCoeffs[0];
            else if (node.SurroundingPhase == descendingPhases[1]) return descendingPhaseCoeffs[1];
            else
            {
                Debug.Assert(node.SurroundingPhase == descendingPhases[2]);
                return descendingPhaseCoeffs[2];
            }
        }

        public double EvaluateAt(CartesianPoint point)
        {
            if (descendingPhases[0].Contains(point)) return descendingPhaseCoeffs[0];
            else if (descendingPhases[1].Contains(point)) return descendingPhaseCoeffs[1];
            else
            {
                Debug.Assert(descendingPhases[2].Contains(point));
                return descendingPhaseCoeffs[2];
            }
        }

        public double EvaluateAt(IPhase phaseAtPoint)
        {
            if (descendingPhases[0] == phaseAtPoint) return descendingPhaseCoeffs[0];
            else if (descendingPhases[1] == phaseAtPoint) return descendingPhaseCoeffs[1];
            else
            {
                Debug.Assert(descendingPhases[2] == phaseAtPoint);
                return descendingPhaseCoeffs[2];
            }
        }

        public double GetJumpCoefficientBetween(PhaseBoundary phaseBoundary)
        {
            return FindPhaseCoeff(phaseBoundary.PositivePhase) - FindPhaseCoeff(phaseBoundary.NegativePhase);
        }

        public bool IsAppliedDueTo(PhaseBoundary phaseBoundary)
        {
            throw new NotImplementedException();
        }

        private int FindPhaseCoeff(IPhase phase)
        {
            for (int i = 0; i < 3; ++i)
            {
                if (descendingPhases[i] == phase) return descendingPhaseCoeffs[i];
            }
            throw new ArgumentException();
        }

        private class PhaseComparer : IComparer<IPhase>
        {
            public int Compare(IPhase x, IPhase y) => y.ID - x.ID; // Descending order
        }
    }
}
