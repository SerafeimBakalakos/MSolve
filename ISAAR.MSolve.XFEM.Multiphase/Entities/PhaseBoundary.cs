using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;

namespace ISAAR.MSolve.XFEM.Multiphase.Entities
{
    public class PhaseBoundary
    {
        public PhaseBoundary(LineSegment2D segment)
        {
            this.Segment = segment;
        }

        public IPhase NegativePhase { get; set; }
        public IPhase PositivePhase { get; set; }

        public LineSegment2D Segment { get; }
    }
}
