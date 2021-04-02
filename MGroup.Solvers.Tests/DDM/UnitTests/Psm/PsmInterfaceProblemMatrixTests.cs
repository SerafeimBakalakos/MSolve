using System;
using System.Collections.Generic;
using System.Text;

using Xunit;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using MGroup.Solvers.DDM;
using MGroup.Solvers.DDM.Dofs;
using MGroup.Solvers.DDM.Psm.Dofs;
using MGroup.Tests.DDM.UnitTests.Models;
using MGroup.Solvers.DDM.Psm.StiffnessMatrices;
using MGroup.Solvers.DDM.StiffnessMatrices;
using MGroup.Solvers.DDM.Psm.InterfaceProblem;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.DDM.Environments;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Providers;
using MGroup.Solvers.Tests;

namespace MGroup.Tests.DDM.UnitTests.Psm
{
	public static class PsmInterfaceProblemMatrixTests
	{
		[Theory]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus)]
		[InlineData(EnvironmentChoice.ManagedParSubSingleClus)]
		public static void TestInterfaceProblemMatrix(EnvironmentChoice env)
		{
			IDdmEnvironment environment = env.Create();
			(IStructuralModel model, IPsmDofSeparator dofSeparator, IPsmMatrixManager matrixManager) = PrepareTest(environment);

			// Initialize interface problem matrix
			var interfaceProblemMatrix = new InterfaceProblemMatrix(environment, model, dofSeparator, matrixManager);

			// Check
			Matrix computedMatrix = Utilities.CreateExplicitMatrix(
				interfaceProblemMatrix.NumRows,
				interfaceProblemMatrix.NumColumns,
				x =>
				{
					var y = Vector.CreateZero(x.Length);
					interfaceProblemMatrix.Multiply(x, y);
					return y;
				});
			Matrix expectedMatrix = Example4x4ExpectedResults.GetInterfaceProblemMatrix();
			double tol = 1E-6;
			Assert.True(expectedMatrix.Equals(computedMatrix, tol));
		}

		private static (IStructuralModel model, IPsmDofSeparator dofSeparator, IPsmMatrixManager matrixManager) PrepareTest(
			IDdmEnvironment environment)
		{
			// Create model
			AllDofs.AddStructuralDofs();
			IStructuralModel model = Example4x4Model.CreateModelHomogeneous();
			Cluster[] clusters = Example4x4Model.CreateClusters(model);
			Example4x4Model.OrderDofs(model);

			#region mock these
			var elementMatrixProvider = new ElementStructuralStiffnessProvider();

			// Separate dofs and calculate the boolean matrices
			var dofSeparator = new PsmDofSeparator(environment, model, clusters);
			dofSeparator.SeparateSubdomainDofsIntoBoundaryInternal();
			dofSeparator.MapBoundaryDofsBetweenClusterSubdomains();

			IPsmMatrixManagerFactory matrixManagerFactory = new PsmMatrixManagerDense.Factory();
			(IMatrixManager matrixManagerBasic, IPsmMatrixManager matrixManagerPsm)
				= matrixManagerFactory.CreateMatrixManagers(model, dofSeparator);

			// Calculate Kff matrices
			foreach (ISubdomain sub in model.Subdomains)
			{
				matrixManagerBasic.BuildKff(sub.ID, sub.FreeDofOrdering, sub.Elements, elementMatrixProvider);
			}

			// Calculate Kbb, Kbi, Kib, inv(Kii) matrices
			foreach (ISubdomain sub in model.Subdomains)
			{
				matrixManagerPsm.ExtractKiiKbbKib(sub.ID);
				matrixManagerPsm.InvertKii(sub.ID);
			}
			#endregion

			return (model, dofSeparator, matrixManagerPsm);
		}
	}
}
