using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Cracks.Geometry
{
    public interface ICrack2D
    {
        TipCoordinateSystem TipSystem { get; }

        double SignedDistanceFromBody(XNode node);

        double SignedDistanceFromBody(XPoint point);
    }
}
