using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM.Multiphase.Enrichment;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;

namespace ISAAR.MSolve.XFEM.Multiphase.Entities
{
    public class PhaseBoundary
    {
        public PhaseBoundary(LineSegment2D segment, IPhase positivePhase, IPhase negativePhase)
        {
            this.Segment = segment;
            this.PositivePhase = positivePhase;
            this.NegativePhase = negativePhase;

            positivePhase.Boundaries.Add(this);
            positivePhase.Neighbors.Add(negativePhase);
            negativePhase.Boundaries.Add(this);
            negativePhase.Neighbors.Add(positivePhase);
        }

        public StepEnrichment Enrichment { get; set; }

        public IPhase NegativePhase { get; }
        public IPhase PositivePhase { get; }

        public LineSegment2D Segment { get; }
    }
}
