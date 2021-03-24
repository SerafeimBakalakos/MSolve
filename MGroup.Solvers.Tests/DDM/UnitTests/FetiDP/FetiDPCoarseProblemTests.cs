using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using MGroup.Solvers.DDM.StiffnessMatrices;
using MGroup.Solvers.DDM.FetiDP.StiffnessMatrices;
using MGroup.Solvers.DDM.FetiDP.Dofs;
using Xunit;
using MGroup.Solvers.DDM.FetiDP.CoarseProblem;
using MGroup.Solvers.DDM.Mappings;
using MGroup.Solvers.DDM.Environments;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers.Tests;

namespace MGroup.Tests.DDM.UnitTests.FetiDP
{
	public static class FetiDPCoarseProblemTests
	{
		[Theory]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.Dense)]
		[InlineData(EnvironmentChoice.ManagedParSubSingleClus, MatrixFormat.Dense)]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.CSparse)]
		[InlineData(EnvironmentChoice.ManagedParSubSingleClus, MatrixFormat.CSparse)]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.CSparseSymmetric)]
		[InlineData(EnvironmentChoice.ManagedParSubSingleClus, MatrixFormat.CSparseSymmetric)]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.SuiteSparseSymmetric)]
		public static void TestGloballKccStarInverse(EnvironmentChoice env, MatrixFormat matrixFormat)
		{
			IProcessingEnvironment environment = env.Create();

			(IStructuralModel model, IFetiDPDofSeparator dofSeparator, IElementMatrixProvider elementMatrixProvider) = 
				FetiDPMatrixManagerTests.PrepareTest(environment);
			IFetiDPMatrixManagerFactory matrixManagerFactory = FetiDPMatrixManagerTests.CreateMatrixManagerFactory(matrixFormat);
			(IMatrixManager matrixManagerBasic, IFetiDPMatrixManager matrixManagerFetiDP)
				= matrixManagerFactory.CreateMatrixManagers(model, dofSeparator);
			IFetiDPCoarseProblemFactory coarseProblemFactory = CreateCoarseProblemFactory(matrixFormat);
			IFetiDPCoarseProblem coarseProblem = coarseProblemFactory.Create(environment, model);

			// Calculate Kff matrices
			foreach (ISubdomain sub in model.Subdomains)
			{
				matrixManagerBasic.BuildKff(sub.ID, sub.FreeDofOrdering, sub.Elements, elementMatrixProvider);
			}

			// Calculate subdomain level matrices
			foreach (ISubdomain sub in model.Subdomains)
			{
				matrixManagerFetiDP.ExtractKrrKccKrc(sub.ID);
				matrixManagerFetiDP.InvertKrr(sub.ID);
				matrixManagerFetiDP.CalcSchurComplementOfRemainderDofs(sub.ID);
			}

			var mappingsLc = new Dictionary<int, BooleanMatrixRowsToColumns>();
			var matricesKccStar = new Dictionary<int, IMatrix>();
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				int s = subdomain.ID;

				mappingsLc[s] = dofSeparator.GetDofMappingCornerGlobalToSubdomain(s);
				matricesKccStar[s] = matrixManagerFetiDP.GetSchurComplementOfRemainderDofs(s);
			}
			coarseProblem.CreateAndInvertCoarseProblemMatrix(mappingsLc, matricesKccStar);

			// Check
			Matrix expectedKccStarInverse = Example4x4ExpectedResults.GetInverseCoarseProblemMatrix();
			int numCornerDofs = expectedKccStarInverse.NumRows;
			Matrix computedKccStarInverse = Utilities.CreateExplicitMatrix(numCornerDofs, numCornerDofs, 
				coarseProblem.MultiplyInverseCoarseProblemMatrixTimes);
			Assert.True(expectedKccStarInverse.Equals(computedKccStarInverse));
		}

		private static IFetiDPCoarseProblemFactory CreateCoarseProblemFactory(MatrixFormat matrixFormat)
		{
			if (matrixFormat == MatrixFormat.Dense)
			{
				return new FetiDPCoarseProblemDense.Factory();
			}
			else if (matrixFormat == MatrixFormat.CSparse)
			{
				return new FetiDPCoarseProblemCSparseNonSymmetric.Factory();
			}
			else if (matrixFormat == MatrixFormat.CSparseSymmetric)
			{
				return new FetiDPCoarseProblemCSparse.Factory();
			}
			else if (matrixFormat == MatrixFormat.SuiteSparseSymmetric)
			{
				return new FetiDPCoarseProblemSuiteSparse.Factory();
			}
			else
			{
				throw new NotImplementedException();
			}
		}
	}

}
