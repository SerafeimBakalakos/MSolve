using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM.Multiphase.Enrichment
{
    public class DauxHeavisideEnrichment : IEnrichment
    {
        private readonly IPhase phasePlus;

        public DauxHeavisideEnrichment(int id, IPhase phasePlus)
        {
            this.ID = id;
            this.phasePlus = phasePlus;
            this.Dof = new EnrichedDof(this, ThermalDof.Temperature);
        }

        public EnrichedDof Dof { get; }

        public int ID { get; }

        public double EvaluateAt(XNode node)
        {
            if (node.SurroundingPhase == phasePlus) return +1;
            else return -1;
        }

        public double EvaluateAt(CartesianPoint point)
        {
            if (phasePlus.Contains(point)) return +1;
            else return -1;
        }

        public double EvaluateAt(IPhase phaseAtPoint)
        {
            if (phaseAtPoint == phasePlus) return +1;
            else return -1;
        }

        public bool IsAppliedDueTo(PhaseBoundary phaseBoundary)
        {
            if ((phaseBoundary.PositivePhase == this.phasePlus) || (phaseBoundary.NegativePhase == this.phasePlus)) return true;
            else return false;
        }

        private static (IPhase minPhase, IPhase maxPhase) FindMinMaxPhases(IPhase phase1, IPhase phase2)
        {
            IPhase minPhase, maxPhase;
            if (phase1.ID < phase2.ID)
            {
                minPhase = phase1;
                maxPhase = phase2;
            }
            else
            {
                minPhase = phase2;
                maxPhase = phase1;
            }
            return (minPhase, maxPhase);
        }
    }
}
