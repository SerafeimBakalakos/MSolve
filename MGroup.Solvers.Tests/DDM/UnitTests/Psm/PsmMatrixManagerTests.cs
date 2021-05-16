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
using MGroup.Solvers.DDM.Environments;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Providers;
using MGroup.Solvers.Tests;

namespace MGroup.Tests.DDM.UnitTests.Psm
{
	public static class PsmMatrixManagerTests
	{
		[Theory]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.Dense)]
		[InlineData(EnvironmentChoice.ManagedParSubSingleClus, MatrixFormat.Dense)]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.CSparse)]
		[InlineData(EnvironmentChoice.ManagedParSubSingleClus, MatrixFormat.CSparse)]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.CSparseSymmetric)]
		[InlineData(EnvironmentChoice.ManagedParSubSingleClus, MatrixFormat.CSparseSymmetric)]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.SuiteSparseSymmetric)]
		public static void TestMatricesKbbKbiKib(EnvironmentChoice env, MatrixFormat matrixFormat)
		{
			IDdmEnvironment environment = env.Create();
			(IStructuralModel model, IPsmDofSeparator dofSeparator, IElementMatrixProvider elementMatrixProvider) = 
				PrepareTest(environment);
			IPsmMatrixManagerFactory matrixManagerFactory = CreateMatrixManagerFactory(matrixFormat);
			(IMatrixManager matrixManagerBasic, IPsmMatrixManager matrixManagerPsm)
				= matrixManagerFactory.CreateMatrixManagers(model, dofSeparator);

			// Calculate Kff matrices
			foreach (ISubdomain sub in model.Subdomains)
			{
				matrixManagerBasic.BuildKff(sub.ID, sub.FreeDofOrdering, sub.Elements, elementMatrixProvider);
			}

			// Calculate Kbb, Kbi, Kib matrices
			foreach (ISubdomain sub in model.Subdomains)
			{
				matrixManagerPsm.ExtractKiiKbbKib(sub.ID);
			}

			// Check
			foreach (ISubdomain sub in model.Subdomains)
			{
				int numBoundaryDofs = dofSeparator.GetSubdomainDofsBoundaryToFree(sub.ID).Length;
				int numInternalDofs = dofSeparator.GetSubdomainDofsInternalToFree(sub.ID).Length;

				Matrix computedKbb = Utilities.CreateExplicitMatrix(numBoundaryDofs, numBoundaryDofs,
					x => matrixManagerPsm.MultiplyKbb(sub.ID, x));
				Matrix computedKbi = Utilities.CreateExplicitMatrix(numBoundaryDofs, numInternalDofs,
					x => matrixManagerPsm.MultiplyKbi(sub.ID, x));
				Matrix computedKib = Utilities.CreateExplicitMatrix(numInternalDofs, numBoundaryDofs,
					x => matrixManagerPsm.MultiplyKib(sub.ID, x));

				Matrix expectedKbb = Example4x4ExpectedResults.GetMatrixKbb(sub.ID);
				Matrix expectedKbi = Example4x4ExpectedResults.GetMatrixKbi(sub.ID);
				Matrix expectedKib = Example4x4ExpectedResults.GetMatrixKib(sub.ID);

				Assert.True(expectedKbb.Equals(computedKbb,1E-06));
				Assert.True(expectedKbi.Equals(computedKbi,1e-06));
				Assert.True(expectedKib.Equals(computedKib,1e-06));
			}
		}

		[Theory]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.Dense)]
		[InlineData(EnvironmentChoice.ManagedParSubSingleClus, MatrixFormat.Dense)]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.CSparse)]
		[InlineData(EnvironmentChoice.ManagedParSubSingleClus, MatrixFormat.CSparse)]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.CSparseSymmetric)]
		[InlineData(EnvironmentChoice.ManagedParSubSingleClus, MatrixFormat.CSparseSymmetric)]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.SuiteSparseSymmetric)]
		public static void TestMatricesKiiInverse(EnvironmentChoice env, MatrixFormat matrixFormat)
		{
			IDdmEnvironment environment = env.Create();
			(IStructuralModel model, IPsmDofSeparator dofSeparator, IElementMatrixProvider elementMatrixProvider) = 
				PrepareTest(environment);
			IPsmMatrixManagerFactory matrixManagerFactory = CreateMatrixManagerFactory(matrixFormat);
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

			// Check
			foreach (ISubdomain sub in model.Subdomains)
			{
				int numInternalDofs = dofSeparator.GetSubdomainDofsInternalToFree(sub.ID).Length;
				Matrix computedKiiInverse = Utilities.CreateExplicitMatrix(numInternalDofs, numInternalDofs,
					x => matrixManagerPsm.MultiplyInverseKii(sub.ID, x));
				Matrix expectedKiiInverse = Example4x4ExpectedResults.GetMatrixKiiInverse(sub.ID);
				Assert.True(expectedKiiInverse.Equals(computedKiiInverse, 1e-06));
			}
		}

		private static IPsmMatrixManagerFactory CreateMatrixManagerFactory(MatrixFormat matrixFormat)
		{
			if (matrixFormat == MatrixFormat.Dense)
			{
				return new PsmMatrixManagerDense.Factory();
			}
			else if (matrixFormat == MatrixFormat.CSparse)
			{
				return new PsmMatrixManagerCSparse.Factory();
			}
			else if (matrixFormat == MatrixFormat.CSparseSymmetric)
			{
				return new PsmMatrixManagerSymmetricCSparse.Factory();
			}
			else if (matrixFormat == MatrixFormat.SuiteSparseSymmetric)
			{
				return new PsmMatrixManagerSymmetricSuiteSparse.Factory();
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		private static (IStructuralModel model, IPsmDofSeparator dofSeparator, IElementMatrixProvider elementMatrixProvider) PrepareTest(
			IDdmEnvironment environment)
		{
			// Create model
			AllDofs.AddStructuralDofs();
			IStructuralModel model = Example4x4Model.CreateModelHomogeneous();
			Cluster[] clusters = Example4x4Model.CreateClusters(model);
			Example4x4Model.OrderDofs(model);

			#region mock these
			// Separate dofs and calculate the boolean matrices
			var dofSeparator = new PsmDofSeparator(environment, model, clusters);
			dofSeparator.SeparateSubdomainDofsIntoBoundaryInternal();
			dofSeparator.MapBoundaryDofsBetweenClusterSubdomains();

			var elementMatrixProvider = new ElementStructuralStiffnessProvider();
			#endregion

			return (model, dofSeparator, elementMatrixProvider);
		}
	}
}
