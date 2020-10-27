using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Geometry.Primitives
{
    public class XPoint
    {
        public XPoint(int dimension)
        {
            this.Dimension = dimension;
        }

        public Dictionary<CoordinateSystem, double[]> Coordinates { get; } = new Dictionary<CoordinateSystem, double[]>();

        public int Dimension { get; }

        public IXFiniteElement Element { get; set; }

        public double[] ShapeFunctions { get; set; }

        public IPhase Phase { get; set; }
    }
}
