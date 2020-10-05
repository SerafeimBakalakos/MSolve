using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Enrichment
{
    public class JunctionEnrichment_v2 : IJunctionEnrichment
    {
        private readonly IPhase[] phases;

        public JunctionEnrichment_v2(int id, PhaseJunction junction, IPhase positivePhase, IPhase negativePhase)
        {
            this.ID = id;
            this.Junction = junction;
            this.PositivePhase = positivePhase;
            this.NegativePhase = negativePhase;
            this.Dof = new EnrichedDof(this, ThermalDof.Temperature);
            this.phases = new IPhase[] { positivePhase, negativePhase };
        }

        public EnrichedDof Dof { get; }

        public int ID { get; }

        public PhaseJunction Junction { get; }

        public IReadOnlyList<IPhase> Phases => throw new NotImplementedException("The third region can be made of many phases");

        public IPhase PositivePhase { get; }
        public IPhase NegativePhase { get; }

        public double EvaluateAt(XNode node) => EvaluateAt(node.SurroundingPhase);

        public double EvaluateAt(CartesianPoint point)
        {
            throw new NotImplementedException("Deprecated. Use EvaluateAt(IPhase phaseAtPoint)");
        }

        public double EvaluateAt(IPhase phaseAtPoint)
        {
            if (phaseAtPoint.ID == PositivePhase.ID) return +1;
            else if (phaseAtPoint.ID == NegativePhase.ID) return -1;
            else return 0.0;
        }

        public double GetJumpCoefficientBetween(PhaseBoundary phaseBoundary)
        {
            return FindPhaseCoeff(phaseBoundary.PositivePhase) - FindPhaseCoeff(phaseBoundary.NegativePhase);
        }

        public bool IsAppliedDueTo(PhaseBoundary phaseBoundary)
        {
            if (phaseBoundary.PositivePhase.ID == this.PositivePhase.ID) return true;
            if (phaseBoundary.NegativePhase.ID == this.PositivePhase.ID) return true;
            if (phaseBoundary.PositivePhase.ID == this.NegativePhase.ID) return true;
            if (phaseBoundary.NegativePhase.ID == this.NegativePhase.ID) return true;
            return false;
        }

        public bool HasSamePhasesAs(JunctionEnrichment_v2 other)
        {
            var thisPhases = new HashSet<IPhase>(this.phases);
            if (thisPhases.SetEquals(other.phases)) return true;
            else return false;
        }

        //TODO: Keep either this or IsAppliedDueTo(PhaseBoundary phaseBoundary) 
        public bool IntroducesJumpBetween(IPhase phase0, IPhase phase1) 
        {
            if (phase0.ID == this.PositivePhase.ID) return true;
            if (phase1.ID == this.PositivePhase.ID) return true;
            if (phase0.ID == this.NegativePhase.ID) return true;
            if (phase1.ID == this.NegativePhase.ID) return true;
            return false;
        }

        private int FindPhaseCoeff(IPhase phase)
        {
            if (phase.ID == PositivePhase.ID) return +1;
            else if (phase.ID == NegativePhase.ID) return -1;
            else return 0;
        }
    }
}
