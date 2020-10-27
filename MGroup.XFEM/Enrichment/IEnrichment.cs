using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Enrichment
{
    public interface IEnrichment
    {
        //Dofs should not be defined by the enrichment function.E.g.the same Heaviside function needs 1 dof in 2D and 3D thermal, 2 in 2D elasticity, 3 in 3D elasticity
        //EnrichedDof Dof { get; } 

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
