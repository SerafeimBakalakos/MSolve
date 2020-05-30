using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;

namespace MGroup.XFEM.Geometry.ConformingMesh
{
    public class TriangleCell2D
    {
        public TriangleCell2D()
        {
            Vertices = new double[3][];
        }

        public TriangleCell2D(double[] point0, double[] point1, double[] point2)
        {
            Vertices = new double[3][] { point0, point1, point2 };
        }

        public IList<double[]> Vertices { get; }

        public double CalcArea()
        {
            double x0 = Vertices[0][0];
            double y0 = Vertices[0][1];
            double x1 = Vertices[1][0];
            double y1 = Vertices[1][1];
            double x2 = Vertices[2][0];
            double y2 = Vertices[2][1];
            return 0.5 * Math.Abs(x0 * (y1 - y2) + x1 * (y2 - y0) + x2 * (y0 - y1));
        }
    }
}
