using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;

//TODO: Some properties only apply to multiphase problems. Move these elsewhere
namespace MGroup.XFEM.Enrichment
{
    public interface IEnrichment
    {
        int ID { get; }

        //TODO: Not sure about this. This necessitates that the enrichment between phase0 and phase1 is different than the one 
        //      between phase0 and phase2. This does not allow step enrichments to be defined as in/out of a phase
        IReadOnlyList<IPhase> Phases { get; }

        double EvaluateAt(XNode node);

        //TODO: Perhaps the argument should be the phase itself. Also the same argument should be used for materials.
        double EvaluateAt(XPoint point);

        EvaluatedFunction EvaluateAllAt(XPoint point);

        double GetJumpCoefficientBetween(PhaseBoundary phaseBoundary);

        bool IsAppliedDueTo(PhaseBoundary phaseBoundary);
    }
}
