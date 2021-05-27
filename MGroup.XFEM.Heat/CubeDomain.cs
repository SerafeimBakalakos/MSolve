using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.LinearAlgebra;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Heat
{
    public class CubeDomain
    {
        public CubeDomain(double[] coordsMin, double[] coordsMax)
        {
            CoordsMin = coordsMin;
            CoordsMax = coordsMax;
        }

        public double[] CoordsMin { get; }

        public double[] CoordsMax { get; }

        /// <summary>
        /// Assumes that <see cref="Cylinder3D.Start"/> in inside the domain.
        /// </summary>
        /// <param name="cylinder"></param>
        /// <returns></returns>
        public IList<Cylinder3D> SplitCylinderPeriodicallyIfOutside(Cylinder3D cylinder) 
        {
            //TODO: Make sure edges are not intersected by the cylinder
            //TODO: Also make sure that the semaining subcube does fits inside the domain

            // Find which face is intersected first by the cylinder axis, if there is such intersection.
            double lambdaMin = double.MaxValue; // intersection = start + lamda * unitDirection
            for (int d = 0; d < 3; ++d)
            {
                if (cylinder.End[d] < CoordsMin[d])
                {
                    double lambda = (CoordsMin[d] - cylinder.Start[d]) / cylinder.DirectionUnit[d];
                    Debug.Assert(lambda > 0);
                    if (lambda < lambdaMin) lambdaMin = lambda;
                }
                if (cylinder.End[d] > CoordsMax[d])
                {
                    double lambda = (CoordsMax[d] - cylinder.Start[d]) / cylinder.DirectionUnit[d];
                    Debug.Assert(lambda > 0);
                    if (lambda < lambdaMin) lambdaMin = lambda;
                }
            }

            if (lambdaMin == double.MaxValue)
            {
                // The cylinder is entirely inside the domain
                return new Cylinder3D[] { cylinder };
            }
            else
            {
                // Find intersection point with this face
                var intersection = new double[3];
                for (int d = 0; d < 3; ++d)
                {
                    intersection[d] = cylinder.Start[d] + lambdaMin * cylinder.DirectionUnit[d];
                }

                // Find the symmetric of the intersection point (w.r.t the origin) on the opposite face
                var intersectionOpposite = new double[3];
                for (int d = 0; d < 3; ++d)
                {
                    intersectionOpposite[d] = -intersection[d];
                }

                // Move the external part of the original cylinder to start from the opposite face, but inwards.
                var externalOpposite = new double[3];
                for (int d = 0; d < 3; ++d)
                {
                    externalOpposite[d] = intersectionOpposite[d] + (cylinder.End[d] - intersection[d]);
                }

                // Return the two subcylinders
                var subCylinder0 = new Cylinder3D(cylinder.Start, intersection, cylinder.Radius);
                var subCylinder1 = new Cylinder3D(externalOpposite, intersectionOpposite, cylinder.Radius);
                return new Cylinder3D[] { subCylinder0, subCylinder1 };
            }
        }
    }
}
