﻿using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.IGA.Entities;
using ISAAR.MSolve.IGA.Entities.Loads;
using ISAAR.MSolve.IGA.Interfaces;
using ISAAR.MSolve.IGA.Problems.SupportiveClasses;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Numerical.LinearAlgebra;

namespace ISAAR.MSolve.IGA.Elements
{
	public class NURBSElement2DCollocation : Element, IStructuralIsogeometricElement
	{
		public NaturalPoint2D CollocationPoint;
		public ElementDimensions ElementDimensions => ElementDimensions.TwoD;
		public IElementDofEnumerator_v2 DofEnumerator { get; set; }
		public bool MaterialModified { get; }

		public Dictionary<int, double> CalculateLoadingCondition(Element element, Edge edge,
			NeumannBoundaryCondition neumann)
		{
			throw new NotImplementedException();
		}

		public Dictionary<int, double> CalculateLoadingCondition(Element element, Face face,
			NeumannBoundaryCondition neumann)
		{
			throw new NotImplementedException();
		}

		public Dictionary<int, double> CalculateLoadingCondition(Element element, Edge edge,
			PressureBoundaryCondition pressure)
		{
			throw new NotImplementedException();
		}

		public Dictionary<int, double> CalculateLoadingCondition(Element element, Face face,
			PressureBoundaryCondition pressure)
		{
			throw new NotImplementedException();
		}

		public void ResetMaterialModified()
		{
			throw new NotImplementedException();
		}

		public Tuple<double[], double[]> CalculateStresses(Element element, double[] localDisplacements,
			double[] localdDisplacements)
		{
			throw new NotImplementedException();
		}

		public double[] CalculateForces(Element element, double[] localDisplacements, double[] localdDisplacements)
		{
			throw new NotImplementedException();
		}

		public double[] CalculateForcesForLogging(Element element, double[] localDisplacements)
		{
			throw new NotImplementedException();
		}

		public double[,] CalculateDisplacementsForPostProcessing(Element element, double[,] localDisplacements)
		{
			throw new NotImplementedException();
		}

		public void ClearMaterialState()
		{
			throw new NotImplementedException();
		}

		public IMatrix StiffnessMatrix(IElement_v2 element)
		{
			var elementCollocation = (NURBSElement2DCollocation) element;

			var nurbs = new NURBS2D(elementCollocation.Patch.DegreeKsi, elementCollocation.Patch.DegreeHeta,
				elementCollocation.Patch.KnotValueVectorKsi, elementCollocation.Patch.KnotValueVectorHeta,
				elementCollocation.CollocationPoint, elementCollocation.ControlPoints);
			
			var jacobianMatrix = CalculateJacobianMatrix(elementCollocation, nurbs);

			var hessianMatrix = CalculateHessian(elementCollocation, nurbs, 0);
			var squareDerivatives = Matrix3by3.CreateFromArray(new double[3, 3]
				{
					{
						jacobianMatrix[0, 0] * jacobianMatrix[0, 0], 2 * jacobianMatrix[0, 0] * jacobianMatrix[0, 1],
						jacobianMatrix[0, 1] * jacobianMatrix[0, 1]
					},


					{
						jacobianMatrix[0, 0] * jacobianMatrix[1, 0],
						jacobianMatrix[0, 0] * jacobianMatrix[1, 1] + jacobianMatrix[0, 1] * jacobianMatrix[1, 0],
						jacobianMatrix[0, 1] * jacobianMatrix[1, 1]
					},

					{jacobianMatrix[0, 0] * jacobianMatrix[0, 0], 2, jacobianMatrix[0, 1] * jacobianMatrix[0, 1]},
				},false);

			var inverseJacobian = jacobianMatrix.Invert();
			var dR = CalculateNaturalDerivatives(nurbs, inverseJacobian);

			var ddR = CalculateNaturalSecondDerivatives(nurbs, hessianMatrix, dR, squareDerivatives);
			
			return CalculateCollocationPointStiffness(elementCollocation, ddR);
		}
		
		public Matrix CalculateCollocationPointStiffness(NURBSElement2DCollocation elementCollocation, double[,] ddR)
		{
			var collocationPointStiffness = Matrix.CreateZero(2, elementCollocation.ControlPoints.Count * 2);

			var E = elementCollocation.Patch.Material.YoungModulus;
			var nu = elementCollocation.Patch.Material.PoissonRatio;
			var temp = E / (1 - nu) / (1 - nu);
			for (int i = 0; i < elementCollocation.ControlPoints.Count * 2; i += 2)
			{
				var index = i / 2;
				collocationPointStiffness[0, i] = (ddR[0, index] + (1 - nu) / 2 * ddR[2, index]) * temp;
				collocationPointStiffness[1, i] = (1 + nu) / 2 * ddR[1, index] * temp;

				collocationPointStiffness[0, i + 1] = (1 + nu) / 2 * ddR[1, index] * temp;
				collocationPointStiffness[1, i + 1] = (ddR[2, index] + (1 - nu) / 2 * ddR[0, index]) * temp;
			}

			return collocationPointStiffness;
		}

		public double[,] CalculateNaturalSecondDerivatives(NURBS2D nurbs, Matrix2D hessianMatrix, double[,] dR,
			Matrix3by3 squareDerivatives)
		{
			var ddR2 = new double[3, nurbs.NurbsSecondDerivativeValueKsi.Rows];
			for (int i = 0; i < ddR2.GetLength(1); i++)
			{
				ddR2[0, i] = hessianMatrix[0, 0] * dR[0, i] + hessianMatrix[0, 1] * dR[1, i];
				ddR2[1, i] = hessianMatrix[1, 0] * dR[0, i] + hessianMatrix[1, 1] * dR[1, i];
				ddR2[2, i] = hessianMatrix[2, 0] * dR[0, i] + hessianMatrix[2, 1] * dR[1, i];
			}

			var ddR = new double[3, nurbs.NurbsSecondDerivativeValueKsi.Rows];
			var squareInvert = squareDerivatives.Invert();
			for (int i = 0; i < ddR.GetLength(1); i++)
			{
				var temp1 = nurbs.NurbsSecondDerivativeValueKsi[i, 0] - ddR2[0, i];
				var temp2 = nurbs.NurbsSecondDerivativeValueHeta[i, 0] - ddR2[1, i];
				var temp3 = nurbs.NurbsSecondDerivativeValueKsiHeta[i, 0] - ddR2[2, i];

				ddR[0, i] = squareInvert[0, 0] * temp1 + squareInvert[0, 1] * temp2 + squareInvert[0, 2] * temp3;
				ddR[1, i] = squareInvert[1, 0] * temp1 + squareInvert[1, 1] * temp2 + squareInvert[1, 2] * temp3;
				ddR[2, i] = squareInvert[2, 0] * temp1 + squareInvert[2, 1] * temp2 + squareInvert[2, 2] * temp3;
			}

			return ddR;
		}

		public double[,] CalculateNaturalDerivatives(NURBS2D nurbs, Matrix2by2 inverseJacobian)
		{
			var dR = new double[2, nurbs.NurbsSecondDerivativeValueKsi.Rows];
			for (int i = 0; i < dR.GetLength(1); i++)
			{
				var dKsi = nurbs.NurbsDerivativeValuesKsi[i, 0];
				var dHeta = nurbs.NurbsDerivativeValuesHeta[i, 0];

				dR[0, i] = inverseJacobian[0, 0] * dKsi + inverseJacobian[0, 1] * dHeta;
				dR[1, i] = inverseJacobian[1, 0] * dKsi + inverseJacobian[1, 1] * dHeta;
			}

			return dR;
		}

		public Matrix2by2 CalculateJacobianMatrix(NURBSElement2DCollocation elementCollocation, NURBS2D nurbs)
		{
			var jacobianMatrix = Matrix2by2.CreateZero();
			for (int k = 0; k < elementCollocation.ControlPoints.Count; k++)
			{
				jacobianMatrix[0, 0] += nurbs.NurbsDerivativeValuesKsi[k, 0] * elementCollocation.ControlPoints[k].X;
				jacobianMatrix[0, 1] += nurbs.NurbsDerivativeValuesKsi[k, 0] * elementCollocation.ControlPoints[k].Y;
				jacobianMatrix[1, 0] += nurbs.NurbsDerivativeValuesHeta[k, 0] * elementCollocation.ControlPoints[k].X;
				jacobianMatrix[1, 1] += nurbs.NurbsDerivativeValuesHeta[k, 0] * elementCollocation.ControlPoints[k].Y;
			}

			return jacobianMatrix;
		}

		public Vector2 CalculateCartesianCollocationPoint(NURBSElement2DCollocation elementCollocation, NURBS2D nurbs)
		{
			var cartesianCollocationPoint = Vector2.CreateZero();
			for (int k = 0; k < elementCollocation.ControlPoints.Count; k++)
			{
				cartesianCollocationPoint[0] += nurbs.NurbsValues[k, 0] * elementCollocation.ControlPoints[k].X;
				cartesianCollocationPoint[1] += nurbs.NurbsValues[k, 0] * elementCollocation.ControlPoints[k].Y;
			}

			return cartesianCollocationPoint;
		}

		public Matrix2D CalculateHessian(NURBSElement2DCollocation shellElement, NURBS2D nurbs, int j)
		{
			Matrix2D hessianMatrix = new Matrix2D(3, 2);
			for (int k = 0; k < shellElement.ControlPoints.Count; k++)
			{
				hessianMatrix[0, 0] += nurbs.NurbsSecondDerivativeValueKsi[k, j] * shellElement.ControlPoints[k].X;
				hessianMatrix[0, 1] += nurbs.NurbsSecondDerivativeValueKsi[k, j] * shellElement.ControlPoints[k].Y;
				hessianMatrix[1, 0] += nurbs.NurbsSecondDerivativeValueHeta[k, j] * shellElement.ControlPoints[k].X;
				hessianMatrix[1, 1] += nurbs.NurbsSecondDerivativeValueHeta[k, j] * shellElement.ControlPoints[k].Y;
				hessianMatrix[2, 0] += nurbs.NurbsSecondDerivativeValueKsiHeta[k, j] * shellElement.ControlPoints[k].X;
				hessianMatrix[2, 1] += nurbs.NurbsSecondDerivativeValueKsiHeta[k, j] * shellElement.ControlPoints[k].Y;
			}

			return hessianMatrix;
		}

		public IMatrix MassMatrix(IElement_v2 element)
		{
			throw new NotImplementedException();
		}

		public IMatrix DampingMatrix(IElement_v2 element)
		{
			throw new NotImplementedException();
		}

		public IList<IList<DOFType>> GetElementDOFTypes(IElement_v2 element)
		{
			throw new NotImplementedException();
		}
	}
}