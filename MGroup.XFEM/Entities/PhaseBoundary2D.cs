using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Entities
{
    public class PhaseBoundary2D
    {
        public PhaseBoundary2D(IImplicitCurve2D geometry, IPhase positivePhase, IPhase negativePhase)
        {
            this.Geometry = geometry;
            this.PositivePhase = positivePhase;
            this.NegativePhase = negativePhase;

            positivePhase.Boundaries.Add(this);
            positivePhase.Neighbors.Add(negativePhase);
            negativePhase.Boundaries.Add(this);
            negativePhase.Neighbors.Add(positivePhase);
        }

        //public IEnrichment StepEnrichment { get; set; }

        public IPhase NegativePhase { get; }
        public IPhase PositivePhase { get; }

        public IImplicitCurve2D Geometry { get; }
    }
}
