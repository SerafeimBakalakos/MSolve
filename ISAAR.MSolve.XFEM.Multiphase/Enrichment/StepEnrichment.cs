using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM.Multiphase.Enrichment
{
    public class StepEnrichment : IEnrichment
    {
        private readonly IPhase minPhase, maxPhase;

        public StepEnrichment(int id, IPhase phase1, IPhase phase2)
        {
            this.ID = id;
            (this.minPhase, this.maxPhase) = FindMinMaxPhases(phase1, phase2);
            this.Dof = new EnrichedDof(this, ThermalDof.Temperature);
        }

        public EnrichedDof Dof { get; }

        public int ID { get; }

        public double PhaseJumpCoefficient => maxPhase.ID - minPhase.ID;

        public IReadOnlyList<IPhase> Phases => new IPhase[] { maxPhase, minPhase };

        public double EvaluateAt(XNode node) => FindPhaseAt(node).ID;

        public double EvaluateAt(CartesianPoint point)
        {
            // Looking in the phase with max ID is more efficient, since the default phase has id=0 and would be slower to search. 
            if (maxPhase.Contains(point)) return maxPhase.ID;
            else return minPhase.ID;
        }

        public double EvaluateAt(IPhase phaseAtPoint) => phaseAtPoint.ID;

        public IPhase FindPhaseAt(XNode node)
        {
            // Looking in the phase with max ID is more efficient, since the default phase has id=0 and would be slower to search. 
            if (maxPhase.ContainedNodes.Contains(node)) return maxPhase;
            else
            {
                //TODO: Perhaps this should be checked in release configs as well
                Debug.Assert(minPhase.ContainedNodes.Contains(node));
                return minPhase;
            }
        }

        public bool IsAppliedDueTo(PhaseBoundary phaseBoundary)
        {
            (IPhase boundaryMinPhase, IPhase boundaryMaxPhase) =
                FindMinMaxPhases(phaseBoundary.PositivePhase, phaseBoundary.NegativePhase);
            return (boundaryMinPhase == this.minPhase) && (boundaryMaxPhase == this.maxPhase);
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
