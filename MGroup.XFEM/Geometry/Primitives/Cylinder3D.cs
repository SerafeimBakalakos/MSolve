using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace MGroup.XFEM.Geometry.Primitives
{
    public class Cylinder3D : ISurface3D
    {
        private readonly double[] start;
        private readonly double[] end;
        private readonly double radius;
        private readonly double length;
        private readonly Vector directionUnit;

        public Cylinder3D(double[] start, double[] end, double radius)
        {
            this.start = start;
            this.end = end;
            this.radius = radius;

            directionUnit = Vector.CreateFromArray(end) - Vector.CreateFromArray(start);
            length = directionUnit.Norm2();
            directionUnit.ScaleIntoThis(1.0 / length);
        }

        public double SignedDistanceOf(double[] point)
        {
            // Find the projection P0 of P onto the cylider's axis AB
            var pA = Vector.CreateFromArray(start);
            var pP = Vector.CreateFromArray(point);
            double proj = (pP - pA) * directionUnit;
            Vector pP0 = pA.Axpy(directionUnit, proj);
            double distanceFromAxis = (pP - pP0).Norm2();
            double distanceFromOutline = distanceFromAxis - radius;

            // The cylinder is not infinite. Distances from the 2 bases must be taken into account.
            if (proj < 0)
            {
                if (distanceFromOutline < 0)
                {
                    return -proj;
                }
                else
                {
                    double distanceFromBase = -proj;
                    return Math.Sqrt(distanceFromOutline * distanceFromOutline + distanceFromBase * distanceFromBase);
                }
            }
            else if (proj > length)
            {
                if (distanceFromOutline < 0)
                {
                    return proj - length;
                }
                else
                {
                    double distanceFromBase = proj - length;
                    return Math.Sqrt(distanceFromOutline * distanceFromOutline + distanceFromBase * distanceFromBase);
                }
            }
            else
            {
                return distanceFromOutline;
            }
        }
    }
}
