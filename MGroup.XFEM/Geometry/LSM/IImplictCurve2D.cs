using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Geometry.LSM
{
    public interface IImplictCurve2D
    {
        double SignedDistanceOf(XPoint point);
    }
}
