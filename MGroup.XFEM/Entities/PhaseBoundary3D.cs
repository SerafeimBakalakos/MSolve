using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Entities
{
    public class PhaseBoundary3D
    {
        public PhaseBoundary3D(IImplicitSurface3D geometry, IPhase3D positivePhase, IPhase3D negativePhase)
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

        public IPhase3D NegativePhase { get; }
        public IPhase3D PositivePhase { get; }

        public IImplicitSurface3D Geometry { get; }
    }
}
