using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Geometry.LSM;

namespace MGroup.XFEM.Entities
{
    public interface IPhaseBoundary : IXDiscontinuity
    {
        int ID { get; }

        IClosedGeometry Geometry { get; }

        EnrichmentItem StepEnrichment { get; set; }

        IPhase NegativePhase { get; set; }
        IPhase PositivePhase { get; set; }

    }
}
