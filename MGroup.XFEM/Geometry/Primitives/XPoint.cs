using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Elements;

namespace MGroup.XFEM.Geometry.Primitives
{
    public class XPoint
    {
        public Dictionary<CoordinateSystem, double[]> Coordinates { get; } = new Dictionary<CoordinateSystem, double[]>();

        public IXFiniteElement Element { get; set; }

        public double[] ShapeFunctions { get; set; }
    }
}
