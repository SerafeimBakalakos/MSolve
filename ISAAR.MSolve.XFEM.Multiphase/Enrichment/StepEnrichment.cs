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
        private readonly PhaseBoundary boundary;
        private readonly IPhase minPhase, maxPhase;

        public StepEnrichment(int id, PhaseBoundary phaseBoundary)
        {
            this.ID = id;
            if (phaseBoundary.PositivePhase.ID < phaseBoundary.NegativePhase.ID)
            {
                this.minPhase = phaseBoundary.PositivePhase;
                this.maxPhase = phaseBoundary.NegativePhase;
            }
            else
            {
                this.minPhase = phaseBoundary.NegativePhase;
                this.maxPhase = phaseBoundary.PositivePhase;
            }
            this.Dof = new EnrichedDof(this, ThermalDof.Temperature);
        }

        public EnrichedDof Dof { get; }

        public int ID { get; }

        public double PhaseJumpCoefficient => maxPhase.ID - minPhase.ID;

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

        public double EvaluateAt(CartesianPoint point)
        {
            // Looking in the phase with max ID is more efficient, since the default phase has id=0 and would be slower to search. 
            if (maxPhase.Contains(point)) return maxPhase.ID;
            else return minPhase.ID;
        }

        public double EvaluateAt(IPhase phaseAtPoint) => phaseAtPoint.ID;

        public bool IsAppliedDueTo(PhaseBoundary phaseBoundary) => phaseBoundary == this.boundary;
    }
}
