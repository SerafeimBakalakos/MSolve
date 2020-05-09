using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Enrichment
{
    public interface IEnrichment
    {
        EnrichedDof Dof { get; }
        int ID { get; }

        //TODO: Not sure about this. This necessitates that the enrichment between phase0 and phase1 is different than the one 
        //      between phase0 and phase2. This does not allow step enrichments to be defined as in/out of a phase
        IReadOnlyList<IPhase> Phases { get; }

        double EvaluateAt(XNode node);

        //TODO: Perhaps the argument should be the phase itself. Also the same argument should be used for materials.
        double EvaluateAt(CartesianPoint point);

        double EvaluateAt(IPhase phaseAtPoint);

        double GetJumpCoefficientBetween(PhaseBoundary phaseBoundary);

        bool IsAppliedDueTo(PhaseBoundary phaseBoundary);
    }
}
