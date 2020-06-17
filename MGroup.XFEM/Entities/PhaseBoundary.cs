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
        public PhaseBoundary(IImplicitGeometry geometry, IPhase positivePhase, IPhase negativePhase)
        {
            this.Geometry = geometry;
            this.PositivePhase = positivePhase;
            this.NegativePhase = negativePhase;

            positivePhase.Boundaries.Add(this);
            positivePhase.Neighbors.Add(negativePhase);
            negativePhase.Boundaries.Add(this);
            negativePhase.Neighbors.Add(positivePhase);
        }

        public IEnrichment StepEnrichment { get; set; }

        public IPhase NegativePhase { get; }
        public IPhase PositivePhase { get; }

        public IImplicitGeometry Geometry { get; }
    }
}
