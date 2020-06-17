using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Enrichment
{
    public class StepEnrichment : IEnrichment
    {
        private readonly IPhase internalPhase, externalPhase;
        private readonly IPhase minPhase, maxPhase;

        public StepEnrichment(int id, IPhase internalPhase, IPhase externalPhase)
        {
            this.ID = id;
            this.internalPhase = internalPhase;
            this.externalPhase = externalPhase;
            (this.minPhase, this.maxPhase) = FindMinMaxPhases(internalPhase, externalPhase);
            this.Dof = new EnrichedDof(this, ThermalDof.Temperature);
        }

        public EnrichedDof Dof { get; }

        public int ID { get; }

        public double PhaseJumpCoefficient => maxPhase.ID - minPhase.ID;

        public IReadOnlyList<IPhase> Phases => new IPhase[] { maxPhase, minPhase };

        public double EvaluateAt(XNode node)
        {
            if (internalPhase.ContainedNodes.Contains(node)) return -1.0;
            else return 1.0;
        }

        public double EvaluateAt(XPoint point)
        {
            //if (point.Phase != null) return EvaluateAt(point.Phase);

            if (internalPhase.Contains(point)) return -1;
            else return +1;
        }

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

        //WARNING: This only works for points that are in one of the 2 phases.
        private double EvaluateAt(IPhase phaseAtPoint)
        {
            if (phaseAtPoint == maxPhase) return -1;
            else
            {
                Debug.Assert(phaseAtPoint == minPhase);
                return +1;
            }
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
