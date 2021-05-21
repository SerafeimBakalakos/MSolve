using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using MGroup.Solvers_OLD.DDM;
using MGroup.Solvers_OLD;
using MGroup.Tests.DDM.UnitTests.Models;
using MGroup.Solvers_OLD.DDM.StiffnessMatrices;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Providers;

namespace MGroup.Tests.DDM.UnitTests.Psm
{
	public static class MatrixManagerTests
	{
		[Theory]
		[InlineData(MatrixFormat.Dense)]
		[InlineData(MatrixFormat.CSparse)]
		[InlineData(MatrixFormat.CSparseSymmetric)]
		[InlineData(MatrixFormat.SuiteSparseSymmetric)]
		public static void TestMatricesKff(MatrixFormat matrixFormat)
		{
			(IStructuralModel model, IElementMatrixProvider elementMatrixProvider) = PrepareTest();

			// Calculate Kff matrices
			IMatrixManager matrixManager = CreateMatrixManagerBasic(matrixFormat, model);
			foreach (ISubdomain sub in model.Subdomains)
			{
				matrixManager.BuildKff(sub.ID, sub.FreeDofOrdering, sub.Elements, elementMatrixProvider);
			}

			// Check
			foreach (ISubdomain sub in model.Subdomains)
			{
				IMatrixView computedKff = matrixManager.GetLinearSystem(sub.ID).Matrix;
				Matrix expectedKff = Example4x4ExpectedResults.GetMatrixKff(sub.ID);
				double tol = 1E-6;
				Assert.True(expectedKff.Equals(computedKff, tol));
			}
		}

		private static IMatrixManager CreateMatrixManagerBasic(MatrixFormat matrixFormat, IStructuralModel model)
		{
			if (matrixFormat == MatrixFormat.Dense)
			{
				return new MatrixManagerDense(model);
			}
			else if (matrixFormat == MatrixFormat.CSparse)
			{
				return new MatrixManagerCsr(model, false);
			}
			else if (matrixFormat == MatrixFormat.CSparseSymmetric)
			{
				return new MatrixManagerCscSymmetric(model);
			}
			else if (matrixFormat == MatrixFormat.SuiteSparseSymmetric)
			{
				return new MatrixManagerCscSymmetric(model);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		private static (IStructuralModel model, IElementMatrixProvider elementMatrixProvider) PrepareTest()
		{
			// Create model
			AllDofs.AddStructuralDofs();
			IStructuralModel model = Example4x4Model.CreateModelHomogeneous();
			Cluster[] clusters = Example4x4Model.CreateClusters(model);
			Example4x4Model.OrderDofs(model);

			#region mock these
			var elementMatrixProvider = new ElementStructuralStiffnessProvider();
			#endregion

			return (model, elementMatrixProvider);
		}
	}
}
