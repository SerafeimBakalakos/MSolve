﻿using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Entities
{
    public class PhaseBoundary2D
    {
        public PhaseBoundary2D(IImplicitCurve2D geometry, IPhase2D positivePhase, IPhase2D negativePhase)
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

        public IPhase2D NegativePhase { get; }
        public IPhase2D PositivePhase { get; }

        public IImplicitCurve2D Geometry { get; }
    }
}
