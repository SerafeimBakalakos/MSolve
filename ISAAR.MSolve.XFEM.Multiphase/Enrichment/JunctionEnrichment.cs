using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Enrichment
{
    public class JunctionEnrichment : IEnrichment
    {
        private readonly IPhase[] descendingPhases;
        private readonly int[] descendingPhaseCoeffs;

        public JunctionEnrichment(int id, PhaseBoundary boundary, IEnumerable<IPhase> allPhases)
        {
            this.ID = id;
            this.Dof = new EnrichedDof(this, ThermalDof.Temperature);
            this.Boundary = boundary;

            int numPhases = allPhases.Count();
            this.descendingPhases = new IPhase[numPhases];
            this.descendingPhaseCoeffs = new int[numPhases];
            this.descendingPhases[0] = boundary.PositivePhase; 
            this.descendingPhaseCoeffs[0] = +1;
            this.descendingPhases[1] = boundary.NegativePhase;
            this.descendingPhaseCoeffs[1] = -1;

            int i = 2;
            foreach (IPhase phase in allPhases)
            {
                if ((phase != boundary.PositivePhase) && (phase != boundary.NegativePhase))
                {
                    this.descendingPhases[i] = phase;
                    this.descendingPhaseCoeffs[i] = 0;
                    ++i;
                }
            }
            Array.Sort(descendingPhases, descendingPhaseCoeffs, new PhaseComparer());
        }

        public EnrichedDof Dof { get; }

        public int ID { get; }

        public PhaseBoundary Boundary { get; }

        public IReadOnlyList<IPhase> Phases => descendingPhases;

        public double EvaluateAt(XNode node)
        {
            for (int i = 0; i < descendingPhases.Length; ++i)
            {
                if (node.SurroundingPhase == descendingPhases[i]) return descendingPhaseCoeffs[i];
            }
            throw new ArgumentException();
        }

        public double EvaluateAt(CartesianPoint point)
        {
            for (int i = 0; i < descendingPhases.Length - 1; ++i)
            {
                if (descendingPhases[i].Contains(point)) return descendingPhaseCoeffs[i];
            }
            return descendingPhaseCoeffs[descendingPhases.Length - 1];
        }

        public double EvaluateAt(IPhase phaseAtPoint)
        {
            //WARNING: This does not work for a blending element that is intersected by another unrelated to the junction interface
            for (int i = 0; i < descendingPhases.Length; ++i)
            {
                if (phaseAtPoint == descendingPhases[i]) return descendingPhaseCoeffs[i];
            }
            throw new ArgumentException();
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
