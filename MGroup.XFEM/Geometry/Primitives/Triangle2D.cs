using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;

namespace MGroup.XFEM.Geometry.Primitives
{
    public class Triangle2D
    {
        public Triangle2D()
        {
            Vertices = new double[3][];
        }

        public Triangle2D(double[] point0, double[] point1, double[] point2)
        {
            Vertices = new double[3][] { point0, point1, point2 };
        }

        public IList<double[]> Vertices { get; }

        public double CalcArea() => CalcArea(Vertices);

        public static double CalcArea(IList<double[]> vertices)
        {
            double x0 = vertices[0][0];
            double y0 = vertices[0][1];
            double x1 = vertices[1][0];
            double y1 = vertices[1][1];
            double x2 = vertices[2][0];
            double y2 = vertices[2][1];
            return 0.5 * Math.Abs(x0 * (y1 - y2) + x1 * (y2 - y0) + x2 * (y0 - y1));
        }

        public static double CalcArea(IList<IPoint> vertices)
        {
            double x0 = vertices[0].X1;
            double y0 = vertices[0].X2;
            double x1 = vertices[1].X1;
            double y1 = vertices[1].X2;
            double x2 = vertices[2].X1;
            double y2 = vertices[2].X2;
            return 0.5 * Math.Abs(x0 * (y1 - y2) + x1 * (y2 - y0) + x2 * (y0 - y1));
        }
    }
}
