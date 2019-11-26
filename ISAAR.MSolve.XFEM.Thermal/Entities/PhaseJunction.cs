using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;

namespace ISAAR.MSolve.XFEM.Thermal.Entities
{
    public class PhaseJunction
    {
        public PhaseJunction(CartesianPoint point, MaterialPhase[] phases)
        {
            this.Point = point;
            this.Phases = phases;
        }

        public CartesianPoint Point { get; }

        public MaterialPhase[] Phases { get; }

    }
}
