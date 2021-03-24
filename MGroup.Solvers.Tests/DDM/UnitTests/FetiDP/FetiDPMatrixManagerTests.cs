using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using MGroup.Solvers.DDM;
using MGroup.Solvers.DDM.Dofs;
using MGroup.Tests.DDM.UnitTests.Models;
using MGroup.Solvers.DDM.StiffnessMatrices;
using MGroup.Solvers.DDM.FetiDP.StiffnessMatrices;
using MGroup.Solvers.DDM.FetiDP.Dofs;
using MGroup.Solvers.DDM.Environments;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Providers;
using MGroup.Solvers.Tests;

namespace MGroup.Tests.DDM.UnitTests.FetiDP
{
	public static class FetiDPMatrixManagerTests
	{
		[Theory]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.Dense)]
		[InlineData(EnvironmentChoice.ManagedParSubSingleClus, MatrixFormat.Dense)]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.CSparse)]
		[InlineData(EnvironmentChoice.ManagedParSubSingleClus, MatrixFormat.CSparse)]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.CSparseSymmetric)]
		[InlineData(EnvironmentChoice.ManagedParSubSingleClus, MatrixFormat.CSparseSymmetric)]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.SuiteSparseSymmetric)]
		public static void TestMatricesKccKcrKrc(EnvironmentChoice env, MatrixFormat matrixFormat)
		{
			IProcessingEnvironment environment = env.Create();
			(IStructuralModel model, IFetiDPDofSeparator dofSeparator, IElementMatrixProvider elementMatrixProvider) 
				= PrepareTest(environment);
			IFetiDPMatrixManagerFactory matrixManagerFactory = CreateMatrixManagerFactory(matrixFormat);
			(IMatrixManager matrixManagerBasic, IFetiDPMatrixManager matrixManagerFetiDP)
				= matrixManagerFactory.CreateMatrixManagers(model, dofSeparator);

			// Calculate Kff matrices
			foreach (ISubdomain sub in model.Subdomains)
			{
				matrixManagerBasic.BuildKff(sub.ID, sub.FreeDofOrdering, sub.Elements, elementMatrixProvider);
			}

			// Calculate Kcc, Kcr, Krc matrices
			foreach (ISubdomain sub in model.Subdomains)
			{
				matrixManagerFetiDP.ExtractKrrKccKrc(sub.ID);
			}

			// Check
			foreach (ISubdomain sub in model.Subdomains)
			{
				int numCornerDofs = dofSeparator.GetDofsCornerToFree(sub.ID).Length;
				int numRemainderDofs = dofSeparator.GetDofsRemainderToFree(sub.ID).Length;

				Matrix computedKcc = Utilities.CreateExplicitMatrix(numCornerDofs, numCornerDofs,
					x => matrixManagerFetiDP.MultiplyKccTimes(sub.ID, x));
				Matrix computedKcr = Utilities.CreateExplicitMatrix(numCornerDofs, numRemainderDofs,
					x => matrixManagerFetiDP.MultiplyKcrTimes(sub.ID, x));
				Matrix computedKrc = Utilities.CreateExplicitMatrix(numRemainderDofs, numCornerDofs,
					x => matrixManagerFetiDP.MultiplyKrcTimes(sub.ID, x));

				Matrix expectedKcc = Example4x4ExpectedResults.GetMatrixKcc(sub.ID);
				Matrix expectedKcr = Example4x4ExpectedResults.GetMatrixKcr(sub.ID);
				Matrix expectedKrc = Example4x4ExpectedResults.GetMatrixKrc(sub.ID);

				Assert.True(expectedKcc.Equals(computedKcc,1e-06));
				Assert.True(expectedKcr.Equals(computedKcr, 1e-06));
				Assert.True(expectedKrc.Equals(computedKrc,1e-06));
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
		public static void TestMatricesKrrInverse(EnvironmentChoice env, MatrixFormat matrixFormat)
		{
			IProcessingEnvironment environment = env.Create();
			(IStructuralModel model, IFetiDPDofSeparator dofSeparator, IElementMatrixProvider elementMatrixProvider) 
				= PrepareTest(environment);
			IFetiDPMatrixManagerFactory matrixManagerFactory = CreateMatrixManagerFactory(matrixFormat);
			(IMatrixManager matrixManagerBasic, IFetiDPMatrixManager matrixManagerFetiDP)
				= matrixManagerFactory.CreateMatrixManagers(model, dofSeparator);

			// Calculate Kff matrices
			foreach (ISubdomain sub in model.Subdomains)
			{
				matrixManagerBasic.BuildKff(sub.ID, sub.FreeDofOrdering, sub.Elements, elementMatrixProvider);
			}

			// Calculate Kcc, Kcr, Krc, inv(Krr) matrices
			foreach (ISubdomain sub in model.Subdomains)
			{
				matrixManagerFetiDP.ExtractKrrKccKrc(sub.ID);
				matrixManagerFetiDP.InvertKrr(sub.ID);
			}

			// Check
			foreach (ISubdomain sub in model.Subdomains)
			{
				int numRemainderDofs = dofSeparator.GetDofsRemainderToFree(sub.ID).Length;
				Matrix computedKrrInverse = Utilities.CreateExplicitMatrix(numRemainderDofs, numRemainderDofs,
					x => matrixManagerFetiDP.MultiplyInverseKrrTimes(sub.ID, x));
				Matrix expectedKrrInverse = Example4x4ExpectedResults.GetMatrixKrrInverse(sub.ID);
				Assert.True(expectedKrrInverse.Equals(computedKrrInverse, 1e-06));
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
		public static void TestMatricesKccStar(EnvironmentChoice env, MatrixFormat matrixFormat)
		{
			IProcessingEnvironment environment = env.Create();
			(IStructuralModel model, IFetiDPDofSeparator dofSeparator, IElementMatrixProvider elementMatrixProvider) 
				= PrepareTest(environment);
			IFetiDPMatrixManagerFactory matrixManagerFactory = CreateMatrixManagerFactory(matrixFormat);
			(IMatrixManager matrixManagerBasic, IFetiDPMatrixManager matrixManagerFetiDP)
				= matrixManagerFactory.CreateMatrixManagers(model, dofSeparator);

			// Calculate Kff matrices
			foreach (ISubdomain sub in model.Subdomains)
			{
				matrixManagerBasic.BuildKff(sub.ID, sub.FreeDofOrdering, sub.Elements, elementMatrixProvider);
			}

			// Calculate Kcc, Kcr, Krc, inv(Krr), KccStar matrices
			foreach (ISubdomain sub in model.Subdomains)
			{
				matrixManagerFetiDP.ExtractKrrKccKrc(sub.ID);
				matrixManagerFetiDP.InvertKrr(sub.ID);
				matrixManagerFetiDP.CalcSchurComplementOfRemainderDofs(sub.ID);
			}

			// Check
			foreach (ISubdomain sub in model.Subdomains)
			{
				Matrix computedKccStar = matrixManagerFetiDP.GetSchurComplementOfRemainderDofs(sub.ID).CopyToFullMatrix();
				Matrix expectedKccStar = Example4x4ExpectedResults.GetMatrixKccStar(sub.ID);
				Assert.True(expectedKccStar.Equals(computedKccStar));
			}
		}

		internal static IFetiDPMatrixManagerFactory CreateMatrixManagerFactory(MatrixFormat matrixFormat)
		{
			if (matrixFormat == MatrixFormat.Dense)
			{
				return new FetiDPMatrixManagerDense.Factory();
			}
			else if (matrixFormat == MatrixFormat.CSparse)
			{
				return new FetiDPMatrixManagerCSparseNonSymmetric.Factory();
			}
			else if (matrixFormat == MatrixFormat.CSparseSymmetric)
			{
				return new FetiDPMatrixManagerCSparse.Factory();
			}
			else if (matrixFormat == MatrixFormat.SuiteSparseSymmetric)
			{
				return new FetiDPMatrixManagerSuiteSparse.Factory();
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		internal static (IStructuralModel model, IFetiDPDofSeparator dofSeparator, IElementMatrixProvider elementMatrixProvider) 
			PrepareTest(IProcessingEnvironment environment)
		{
			// Create model
			AllDofs.AddStructuralDofs();
			IStructuralModel model = Example4x4Model.CreateModelHomogeneous();
			Cluster[] clusters = Example4x4Model.CreateClusters(model);
			ICornerDofSelection cornerDofs = Example4x4Model.CreateCornerNodes();
			Example4x4Model.OrderDofs(model);

			#region mock these
			// Separate dofs and calculate the boolean matrices
			var dofSeparator = new FetiDPDofSeparator(environment, model, clusters);
			dofSeparator.SeparateCornerRemainderDofs(cornerDofs);
			dofSeparator.OrderGlobalCornerDofs();
			dofSeparator.MapCornerDofs();

			var elementMatrixProvider = new ElementStructuralStiffnessProvider();
			#endregion

			return (model, dofSeparator, elementMatrixProvider);
		}
	}
}
