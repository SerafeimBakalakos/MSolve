using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Enrichment.Functions;
using MGroup.XFEM.Geometry.LSM;

namespace MGroup.XFEM.Entities
{
    public interface IPhaseBoundary : IXDiscontinuity
    {
        int ID { get; }

        IClosedGeometry Geometry { get; }

        PhaseStepEnrichment StepEnrichment { get; }

        IPhase NegativePhase { get; set; }
        IPhase PositivePhase { get; set; }

    }
}
