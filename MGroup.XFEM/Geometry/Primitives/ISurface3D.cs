﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.XFEM.Geometry.Primitives
{
    public interface ISurface3D
    {
        double SignedDistanceOf(double[] point);

        IElementCurveIntersection2D IntersectPolygon(IList<double[]> nodes);
    }
}
