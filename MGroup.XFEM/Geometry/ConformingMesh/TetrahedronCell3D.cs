using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;

namespace MGroup.XFEM.Geometry.ConformingMesh
{
    public class TetrahedronCell3D
    {
        public TetrahedronCell3D()
        {
            Vertices = new double[4][];
        }

        public TetrahedronCell3D(double[] point0, double[] point1, double[] point2, double[] point3)
        {
            Vertices = new double[4][] { point0, point1, point2, point3 };
        }

        public IList<double[]> Vertices { get; }

        public double CalcVolume()
        {
            double x0 = Vertices[0][0];
            double y0 = Vertices[0][1];
            double z0 = Vertices[0][2];

            double x1 = Vertices[1][0];
            double y1 = Vertices[1][1];
            double z1 = Vertices[1][2];

            double x2 = Vertices[2][0];
            double y2 = Vertices[2][1];
            double z2 = Vertices[2][2];

            double x3 = Vertices[3][0];
            double y3 = Vertices[3][1];
            double z3 = Vertices[3][2];

            var matrix = Matrix.CreateZero(3, 3);
            matrix[0, 0] = x0 - x3;
            matrix[1, 0] = y0 - y3;
            matrix[2, 0] = z0 - z3;

            matrix[0, 1] = x1 - x3;
            matrix[1, 1] = y1 - y3;
            matrix[2, 1] = z1 - z3;

            matrix[0, 2] = x2 - x3;
            matrix[1, 2] = y2 - y3;
            matrix[2, 2] = z2 - z3;

            double det = matrix.CalcDeterminant();
            return det / 6.0;
        }
    }
}
