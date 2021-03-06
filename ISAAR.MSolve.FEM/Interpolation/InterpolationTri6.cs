﻿using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.FEM.Interpolation.Inverse;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;

// Tri6 nodes:
// 1
// | \
// 4   3
// |     \
// 2 - 5 - 0

//TODO: See https://www.colorado.edu/engineering/CAS/courses.d/IFEM.d/IFEM.Ch24.d/IFEM.Ch24.pdf for optimizations
namespace ISAAR.MSolve.FEM.Interpolation
{
    /// <summary>
    /// Isoparametric interpolation of a triangular finite element with 6 nodes. Quadratic shape functions.
    /// The shape function computations are presented in Papadrakakis book (2001 print) pages 230-232.
    /// Implements Singleton pattern. 
    /// Authors: Serafeim Bakalakos
    /// </summary>
    public class InterpolationTri6 : IsoparametricInterpolation2DBase
    {
        private static readonly InterpolationTri6 uniqueInstance = new InterpolationTri6();

        private InterpolationTri6() : base(CellType2D.Tri6, 6)
        {
            NodalNaturalCoordinates = new NaturalPoint2D[]
            {
                new NaturalPoint2D(1.0, 0.0),
                new NaturalPoint2D(0.0, 1.0),
                new NaturalPoint2D(0.0, 0.0),

                new NaturalPoint2D(0.5, 0.5),
                new NaturalPoint2D(0.0, 0.5),
                new NaturalPoint2D(0.5, 0.0)
            };
        }

        /// <summary>
        /// The coordinates of the finite element's nodes in the natural (element local) coordinate system. The order of these
        /// nodes matches the order of the shape functions and is always the same for each element.
        /// </summary>
        public override IReadOnlyList<NaturalPoint2D> NodalNaturalCoordinates { get; }

        /// <summary>
        /// Get the unique <see cref="InterpolationTri6"/> object for the whole program. Thread safe.
        /// </summary>
        public static InterpolationTri6 UniqueInstance => uniqueInstance;

        /// <summary>
        /// The inverse mapping of this interpolation, namely from global cartesian to natural (element local) coordinate system.
        /// </summary>
        /// <param name="nodes">The nodes of the finite element in the global cartesian coordinate system.</param>
        /// <returns></returns>
        public override IInverseInterpolation2D CreateInverseMappingFor(IReadOnlyList<Node2D> nodes)
            => throw new NotImplementedException("Requires an iterative procedure.");

        protected override sealed double[] EvaluateAt(double xi, double eta)
        {
            // Area coordinates
            double s1 = xi;
            double s2 = eta;
            double s3 = 1 - xi - eta;

            var values = new double[6];
            values[0] = s1 * (2 * s1 - 1);
            values[1] = s2 * (2 * s2 - 1);
            values[2] = s3 * (2 * s3 - 1);

            values[3] = 4 * s1 * s2;
            values[4] = 4 * s2 * s3;
            values[5] = 4 * s3 * s1;
            return values;
        }

        protected override sealed double[,] EvaluateGradientsAt(double xi, double eta)
        {
            // Area coordinates
            double s1 = xi;
            double s2 = eta;
            double s3 = 1 - xi - eta;

            var derivatives = new double[6, 2];
            derivatives[0, 0] = 4 * s1 - 1;
            derivatives[0, 1] = 0.0;
            derivatives[1, 0] = 0.0;
            derivatives[1, 1] = 4 * s2 - 1;
            derivatives[2, 0] = -4 * s3 + 1;
            derivatives[2, 1] = -4 * s3 + 1;

            derivatives[3, 0] = 4 * s2;
            derivatives[3, 1] = 4 * s1;
            derivatives[4, 0] = -4 * s2;
            derivatives[4, 1] = 4 * (s3 - s2);
            derivatives[5, 0] = 4 * (s3 - s1);
            derivatives[5, 1] = -4 * s1;
            return derivatives;
        }
    }
}
