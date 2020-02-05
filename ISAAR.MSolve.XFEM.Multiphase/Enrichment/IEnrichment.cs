using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM.Multiphase.Enrichment
{
    public interface IEnrichment
    {
        EnrichedDof Dof { get; }
        int ID { get; }

        double EvaluateAt(XNode node);

        //TODO: Perhaps the argument should be the phase itself. Also the same argument should be used for materials.
        double EvaluateAt(CartesianPoint point);

        double EvaluateAt(IPhase phaseAtPoint);

        bool IsAppliedDueTo(PhaseBoundary phaseBoundary);
    }
}
