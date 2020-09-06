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
        private readonly IPhase positivePhase, negativePhase;
        private readonly IPhase[] phases;

        public JunctionEnrichment_v2(int id, IPhase positivePhase, IPhase negativePhase)
        {
            this.ID = id;
            this.positivePhase = positivePhase;
            this.negativePhase = negativePhase;
            this.Dof = new EnrichedDof(this, ThermalDof.Temperature);
            this.phases = new IPhase[] { positivePhase, negativePhase };
        }

        public EnrichedDof Dof { get; }

        public int ID { get; }

        public IReadOnlyList<IPhase> Phases => throw new NotImplementedException("The third region can be made of many phases");

        public double EvaluateAt(XNode node) => EvaluateAt(node.SurroundingPhase);

        public double EvaluateAt(CartesianPoint point)
        {
            throw new NotImplementedException();
        }

        public double EvaluateAt(IPhase phaseAtPoint)
        {
            if (phaseAtPoint.ID == positivePhase.ID) return +1;
            else if (phaseAtPoint.ID == negativePhase.ID) return -1;
            else return 0.0;
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
            if (phase.ID == positivePhase.ID) return +1;
            else if (phase.ID == negativePhase.ID) return -1;
            else return 0;
        }
    }
}
