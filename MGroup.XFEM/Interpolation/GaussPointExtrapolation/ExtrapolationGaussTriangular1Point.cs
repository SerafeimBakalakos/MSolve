﻿using ISAAR.MSolve.Geometry.Coordinates;
using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Commons;
using MGroup.XFEM.Integration.Quadratures;

namespace MGroup.XFEM.Interpolation.GaussPointExtrapolation
{
    /// Calculates extrapolations of scalar, vector and tensor fields from the integration points of symmetric Gauss quadrature
    /// for triangles with 1 Gauss point. This can be done at any point, but utility methods for directly outputting the 
    /// extrapolated fields at the nodes of finite elements are also provided. Note that since there is only 1 Gauss point,
    /// the scalar, vector and tensor fields are constant at all points and equal to their values at the Gauss point.
    /// Implements Singleton pattern.
    /// </summary>
    public class ExtrapolationGaussTriangular1Point : IGaussPointExtrapolation
    {
        private static readonly ExtrapolationGaussTriangular1Point uniqueInstance = new ExtrapolationGaussTriangular1Point();

        /// <summary>
        /// See <see cref="IGaussPointExtrapolation2D.Quadrature"/>
        /// </summary>
        public IQuadrature Quadrature { get { return TriangleQuadratureSymmetricGaussian.Order1Point1; } }

        /// <summary>
        /// Get the unique <see cref="ExtrapolationGaussTriangular1Point"/> object for the whole program. Thread safe.
        /// </summary>
        public static ExtrapolationGaussTriangular1Point UniqueInstance { get { return uniqueInstance; } }

        /// <summary>
        /// See <see cref="IGaussPointExtrapolation2D.ExtrapolateScalarFromGaussPoints(IReadOnlyList{double}, double[])"/>.
        /// </summary>
        public double ExtrapolateScalarFromGaussPoints(IReadOnlyList<double> scalarsAtGaussPoints, double[] naturalPoint)
            => scalarsAtGaussPoints[0];

        /// <summary>
        /// See <see cref="IGaussPointExtrapolation2D.ExtrapolateScalarFromGaussPointsToNodes(
        /// IReadOnlyList{double}, IIsoparametricInterpolation)"/>
        /// </summary>
        public IReadOnlyList<double> ExtrapolateScalarFromGaussPointsToNodes(IReadOnlyList<double> scalarsAtGaussPoints, 
            IIsoparametricInterpolation interpolation)
        {
            var nodalScalars = new double[interpolation.NumFunctions];
            for (int i = 0; i < nodalScalars.Length; ++i) nodalScalars[i] = scalarsAtGaussPoints[0];
            return nodalScalars;
        }

        /// <summary>
        /// See <see cref="IGaussPointExtrapolation2D.ExtrapolateTensorFromGaussPoints(IReadOnlyList{double[]}, double[])"/>.
        /// </summary>
        public double[] ExtrapolateTensorFromGaussPoints(IReadOnlyList<double[]> tensorsAtGaussPoints, double[] naturalPoint)
            => tensorsAtGaussPoints[0];

        /// <summary>
        /// See <see cref="IGaussPointExtrapolation2D.ExtrapolateTensorFromGaussPoints(IReadOnlyList{Tensor2D}, double[])"/>.
        /// </summary>
        public Tensor2D ExtrapolateTensorFromGaussPoints(IReadOnlyList<Tensor2D> tensorsAtGaussPoints, double[] naturalPoint)
            => tensorsAtGaussPoints[0];

        /// <summary>
        /// See <see cref="IGaussPointExtrapolation2D.ExtrapolateTensorFromGaussPointsToNodes(
        /// IReadOnlyList{double[]}, IIsoparametricInterpolation)"/>
        /// </summary>
        public IReadOnlyList<double[]> ExtrapolateTensorFromGaussPointsToNodes(IReadOnlyList<double[]> tensorsAtGaussPoints,
            IIsoparametricInterpolation interpolation)
        {
            var nodalTensors = new double[interpolation.NumFunctions][];
            for (int i = 0; i < nodalTensors.Length; ++i) nodalTensors[i] = tensorsAtGaussPoints[0];
            return nodalTensors;
        }

        /// <summary>
        /// See <see cref="IGaussPointExtrapolation2D.ExtrapolateTensorFromGaussPointsToNodes(
        /// IReadOnlyList{Tensor2D}, IIsoparametricInterpolation)"/>
        /// </summary>
        public IReadOnlyList<Tensor2D> ExtrapolateTensorFromGaussPointsToNodes(IReadOnlyList<Tensor2D> tensorsAtGaussPoints, 
            IIsoparametricInterpolation interpolation)
        {
            var nodalTensors = new Tensor2D[interpolation.NumFunctions];
            for (int i = 0; i < nodalTensors.Length; ++i) nodalTensors[i] = tensorsAtGaussPoints[0];
            return nodalTensors;
        }

        /// <summary>
        /// See <see cref="IGaussPointExtrapolation2D.ExtrapolateVectorFromGaussPoints(IReadOnlyList{double[]}, double[])"/>.
        /// </summary>
        public double[] ExtrapolateVectorFromGaussPoints(IReadOnlyList<double[]> vectorsAtGaussPoints, double[] naturalPoint)
            => vectorsAtGaussPoints[0];

        /// <summary>
        /// See <see cref="IGaussPointExtrapolation2D.ExtrapolateVectorFromGaussPointsToNodes(
        /// IReadOnlyList{double[]}, IIsoparametricInterpolation)"/>
        /// </summary>
        public IReadOnlyList<double[]> ExtrapolateVectorFromGaussPointsToNodes(IReadOnlyList<double[]> vectorsAtGaussPoints, 
            IIsoparametricInterpolation interpolation)
        {
            var nodalVectors = new double[interpolation.NumFunctions][];
            for (int i = 0; i < nodalVectors.Length; ++i) nodalVectors[i] = vectorsAtGaussPoints[0];
            return nodalVectors;
        }
    }
}
