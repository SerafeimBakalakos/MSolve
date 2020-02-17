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

        public double EvaluateAt(XNode node) => EvaluateAt(node.SurroundingPhase);

        public double EvaluateAt(CartesianPoint point)
        {
            // Looking in the phase with max ID is more efficient, since the default phase has id=0 and would be slower to search. 
            if (maxPhase.Contains(point)) return +1;
            else return -1;
        }

        public double EvaluateAt(IPhase phaseAtPoint)
        {
            if (phaseAtPoint == maxPhase) return +1;
            else
            {
                #region debug
                // Uncomment the next when done
                #endregion
                //Debug.Assert(phaseAtPoint == minPhase);
                return -1;
            }
        }

        #region delete
        //public IPhase FindPhaseAt(XNode node)
        //{
        //    return node.SurroundingPhase;
        //    //// Looking in the phase with max ID is more efficient, since the default phase has id=0 and would be slower to search. 
        //    //if (maxPhase.ContainedNodes.Contains(node)) return maxPhase;
        //    //else
        //    //{
        //    //    //TODO: Perhaps this should be checked in release configs as well
        //    //    Debug.Assert(minPhase.ContainedNodes.Contains(node));
        //    //    return minPhase;
        //    //}
        //}
        #endregion

        public double GetJumpCoefficientBetween(PhaseBoundary phaseBoundary)
        {
            (IPhase boundaryMinPhase, IPhase boundaryMaxPhase) =
                FindMinMaxPhases(phaseBoundary.PositivePhase, phaseBoundary.NegativePhase);
            if ((boundaryMinPhase == this.minPhase) && (boundaryMaxPhase == this.maxPhase))
            {
                return EvaluateAt(phaseBoundary.PositivePhase) - EvaluateAt(phaseBoundary.NegativePhase);
            }
            else return 0.0;
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
