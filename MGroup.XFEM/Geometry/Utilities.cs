using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra.Commons;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Geometry
{
    public static class Utilities
    {
        public static double CalcPolygonArea(IList<double[]> vertices)
        {
            double sum = 0.0;
            for (int i = 0; i < vertices.Count; ++i)
            {
                double[] pointA = vertices[i];
                double[] pointB = vertices[(i + 1) % vertices.Count];
                sum += pointA[0] * pointB[1] - pointB[0] * pointA[1];
            }
            return 0.5 * Math.Abs(sum);
        }

        public static double CalcTetrahedronVolume(IList<double[]> vertices)
        {
            double x0 = vertices[0][0];
            double y0 = vertices[0][1];
            double z0 = vertices[0][2];

            double x1 = vertices[1][0];
            double y1 = vertices[1][1];
            double z1 = vertices[1][2];

            double x2 = vertices[2][0];
            double y2 = vertices[2][1];
            double z2 = vertices[2][2];

            double x3 = vertices[3][0];
            double y3 = vertices[3][1];
            double z3 = vertices[3][2];

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

        public static double Distance2D(double[] pointA, double[] pointB)
        {
            double dx0 = pointB[0] - pointA[0];
            double dx1 = pointB[1] - pointA[1];
            return Math.Sqrt(dx0 * dx0 + dx1 * dx1);
        }

        public static double Distance3D(double[] pointA, double[] pointB)
        {
            double dx0 = pointB[0] - pointA[0];
            double dx1 = pointB[1] - pointA[1];
            double dx2 = pointB[2] - pointA[2];
            return Math.Sqrt(dx0 * dx0 + dx1 * dx1 + dx2 * dx2);
        }

        public static double[] FindCentroid(IReadOnlyList<double[]> vertices)
        {
            int dimension = vertices[0].Length;
            var centroid = new double[dimension];
            foreach (double[] vertex in vertices)
            {
                for (int d = 0; d < dimension; ++d)
                {
                    centroid[d] += vertex[d];
                }
            }
            for (int d = 0; d < dimension; ++d)
            {
                centroid[d] /= vertices.Count;
            }
            return centroid;
        }

        public static double[] FindCentroid(IList<double[]> vertices)
        {
            int dimension = vertices[0].Length;
            var centroid = new double[dimension];
            foreach (double[] vertex in vertices)
            {
                for (int d = 0; d < dimension; ++d)
                {
                    centroid[d] += vertex[d];
                }
            }
            for (int d = 0; d < dimension; ++d)
            {
                centroid[d] /= vertices.Count;
            }
            return centroid;
        }

        public static double[] FindCentroidCartesian(int dimension, IReadOnlyList<XNode> nodes)
        {
            var centroid = new double[dimension];
            for (int n = 0; n < nodes.Count; ++n)
            {
                for (int d = 0; d < dimension; ++d)
                {
                    centroid[d] += nodes[n].Coordinates[d];
                }
            }
            for (int d = 0; d < dimension; ++d)
            {
                centroid[d] /= nodes.Count;
            }
            return centroid;
        }

        public static bool HaveCommonEntries<T>(HashSet<T> set0, HashSet<T> set1)
        {
            foreach (var entry in set0)
            {
                if (set1.Contains(entry)) return true;
            }
            return false;
        }

        public static int IndexOfMinAbs(IReadOnlyList<double> values)
        {
            double min = double.MaxValue;
            int pos = -1;
            for (int i = 0; i < values.Count; ++i)
            {
                double absDistance = Math.Abs(values[i]);
                if (absDistance < min)
                {
                    min = absDistance;
                    pos = i;
                }
            }
            return pos;
        }

        public static int[] OffsetArray(int[] original, int offset)
        {
            var result = new int[original.Length];
            for (int i = 0; i < original.Length; i++)
            {
                result[i] = original[i] + offset;
            }
            return result;
        }

        public static bool PointsCoincide(double[] point0, double[] point1, ValueComparer comparer)
        {
            if (point0.Length != point1.Length) throw new ArgumentException("The 2 points have different number of coordinates");
            for (int d = 0; d < point0.Length; ++d)
            {
                if (!comparer.AreEqual(point0[d], point1[d]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Wraps a counter-clockwise angle in (-pi, pi]
        /// <returns></returns>
        public static double WrapAngle(double angle)
        {
            // TODO: (-pi, pi] perhaps is not the best range to work with. 
            // It is convenient in that atan2 returns values there and negative angles are non convex, but the interval should 
            // be closed on the lower bound, to match other formulas. Overall [0, 2pi) seems better overall.

            double twoPI = 2.0 * Math.PI;
            // Wrap to [0, 2pi)
            double quotient = Math.Floor(angle / twoPI);
            double modulus = angle - twoPI * quotient;
            // Wrap to (-pi, pi]
            double excess = modulus - Math.PI;
            if (excess > 0) // (pi, 2pi) -> (-pi, 0). The [0, pi] is not affected.
            {
                modulus = -Math.PI + excess;
            }
            return modulus;
        }
    }
}
