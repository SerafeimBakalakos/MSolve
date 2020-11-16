using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Entities
{
    public class PhaseBoundary
    {
        public PhaseBoundary(IClosedGeometry geometry, IPhase positivePhase, IPhase negativePhase)
        {
            this.Geometry = geometry;
            this.PositivePhase = positivePhase;
            this.NegativePhase = negativePhase;
        }

        public IEnrichmentFunction StepEnrichment { get; set; }

        public IPhase NegativePhase { get; set; }
        public IPhase PositivePhase { get; set; }

        public IClosedGeometry Geometry { get; set; }
    }
}
