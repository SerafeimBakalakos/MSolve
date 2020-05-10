using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.XFEM.Geometry.Primitives
{
    public static class VectorExtensions
    {
        public static double[] Add2D(this double[] thisVector, double[] otherVector)
        {
            return new double[] 
            { 
                thisVector[0] + otherVector[0], thisVector[1] + otherVector[1]
            };
        }

        public static double[] Add3D(this double[] thisVector, double[] otherVector)
        {
            return new double[]
            {
                thisVector[0] + otherVector[0], thisVector[1] + otherVector[1], thisVector[2] + otherVector[2]
            };
        }

        public static double Distance2D(this double[] thisPoint, double[] otherPoint)
        {
            double dx0 = otherPoint[0] - thisPoint[0];
            double dx1 = otherPoint[1] - thisPoint[1];
            return Math.Sqrt(dx0 * dx0 + dx1 * dx1);
        }

        public static double Distance3D(this double[] thisPoint, double[] otherPoint)
        {
            double dx0 = otherPoint[0] - thisPoint[0];
            double dx1 = otherPoint[1] - thisPoint[1];
            double dx2 = otherPoint[2] - thisPoint[2];
            return Math.Sqrt(dx0 * dx0 + dx1 * dx1 + dx2 * dx2);
        }

        public static double DotProduct2D(this double[] thisVector, double[] otherVector)
        {
            return thisVector[0] * otherVector[0] + thisVector[1] * otherVector[1];
        }

        public static double DotProduct3D(this double[] thisVector, double[] otherVector)
        {
            return thisVector[0] * otherVector[0] + thisVector[1] * otherVector[1] + thisVector[2] * otherVector[2];
        }

        public static double Norm2D(this double[] thisVector)
        {
            return Math.Sqrt(thisVector[0] * thisVector[0] + thisVector[1] * thisVector[1]);
        }

        public static double Norm3D(this double[] thisVector)
        {
            return Math.Sqrt(thisVector[0] * thisVector[0] + thisVector[1] * thisVector[1] + thisVector[2] * thisVector[2]);
        }

        public static double[] Scale2D(this double[] thisVector, double scalar)
        {
            return new double[]
            {
                scalar * thisVector[0], scalar * thisVector[1]
            };
        }

        public static double[] Scale3D(this double[] thisVector, double scalar)
        {
            return new double[]
            {
                scalar * thisVector[0], scalar * thisVector[1], scalar * thisVector[2]
            };
        }

        public static double[] Subtract2D(this double[] thisVector, double[] otherVector)
        {
            return new double[]
            {
                thisVector[0] - otherVector[0], thisVector[1] - otherVector[1]
            };
        }

        public static double[] Subtract3D(this double[] thisVector, double[] otherVector)
        {
            return new double[]
            {
                thisVector[0] - otherVector[0], thisVector[1] - otherVector[1], thisVector[2] - otherVector[2]
            };
        }
    }
}
