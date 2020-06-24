﻿using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.FEM.Entities;
using MGroup.XFEM.Interpolation.Inverse;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using System;
using System.Collections.Generic;
using ISAAR.MSolve.Discretization.Interfaces;

namespace MGroup.XFEM.Interpolation
{
	/// <summary>
	/// Isoparametric interpolation of a tetrahedral finite element with 4 nodes. Linear shape functions.
	/// Implements sigleton pattern.
	/// </summary>
	public class InterpolationTet4 : IsoparametricInterpolationBase
    {
		private static readonly  InterpolationTet4 uniqueInstance= new InterpolationTet4();

		private InterpolationTet4() : base(3, CellType.Tet4, 4)
	    {
			NodalNaturalCoordinates = new double[][]
			{
				new double[] { 0,0,0 },
				new double[] { 1,0,0 },
				new double[] { 0,1,0 },
				new double[] { 0,0,1 },
			};
	    }

		/// <summary>
		/// The coordinates of the finite element's nodes in the natural (element local) coordinate system. The order
		/// of these nodes matches the order of the shape functions and is always the same for each element.
		/// </summary>
	    public override IReadOnlyList<double[]> NodalNaturalCoordinates { get; }

		/// <summary>
		/// Get the unique instance <see cref="InterpolationTet4"/> object for the whole program. Thread safe.
		/// </summary>
	    public static InterpolationTet4 UniqueInstance => uniqueInstance;

        /// <summary>
        /// See <see cref="IIsoparametricInterpolation.CheckElementNodes(IReadOnlyList{INode})"/>
        /// </summary>
        public override void CheckElementNodes(IReadOnlyList<INode> nodes)
        {
            if (nodes.Count != 4) throw new ArgumentException(
                $"A Tetra4 finite element has 4 nodes, but {nodes.Count} nodes were provided.");
            // TODO: Also check the order of the nodes too and perhaps even the shape
        }

        /// <summary>
        /// The inverse mapping of this interpolation, namely from global cartesian to natural (element local) coordinate system.
        /// </summary>
        /// <param name="node">The nodes of the finite element in the global cartesian coordinate system.</param>
        /// <returns></returns>
        // TODO: Find and implement inverse mapping for Tet4.
        public override IInverseInterpolation CreateInverseMappingFor(IReadOnlyList<INode> node) 
            => throw new NotImplementedException("Not implemented yet.");

		/// <summary>
		/// Returns the shape functions a tetrahedral linear element evaluated on a single point.
		/// Implementation is based on <see cref="https://www.colorado.edu/engineering/CAS/courses.d/AFEM.d/AFEM.Ch09.d/AFEM.Ch09.pdf">Carlos Felippa - Introduction to Finite Element Methods</see>
		/// </summary>
		/// <param name="xi"></param>
		/// <param name="eta"></param>
		/// <param name="zeta"></param>
		/// <returns></returns>
		protected sealed override double[] EvaluateAt(double[] naturalPoint)
	    {
			double xi = naturalPoint[0];
			double eta = naturalPoint[1];
			double zeta = naturalPoint[2];
			var values = new double[4];
		    values[0] = 1 - xi - eta - zeta;
			values[1] = xi;
		    values[2] = eta;
		    values[3] = zeta;
		    
		    return values;
	    }

	    protected sealed override Matrix EvaluateGradientsAt(double[] naturalPoint)
	    {
		    var derivatives = Matrix.CreateZero(4, 3);
		    derivatives[0, 0] = -1.0;
		    derivatives[0, 1] = -1.0;
		    derivatives[0, 2] = -1.0;

			derivatives[1, 0] = 1.0;
		    derivatives[1, 1] = 0.0;
		    derivatives[1, 2] = 0.0;

			derivatives[2, 0] = 0.0;
		    derivatives[2, 1] = 1.0;
		    derivatives[2, 2] = 0.0;

			derivatives[3, 0] = 0.0;
		    derivatives[3, 1] = 0.0;
		    derivatives[3, 2] = 1.0;
		    
		    return derivatives;
	    }
    }
}
