using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.DDM;
using MGroup.Solvers.DDM.Dofs;
using MGroup.Solvers.DDM.Environments;
using MGroup.Solvers.DDM.FetiDP.CoarseProblem;
using MGroup.Solvers.DDM.FetiDP.Dofs;
using MGroup.Solvers.DDM.Mappings;
using MGroup.Solvers.DDM.PFetiDP.Dofs;
using MGroup.Solvers.DDM.PFetiDP.Preconditioner;
using MGroup.Solvers.DDM.PFetiDP.StiffnessMatrices;
using MGroup.Solvers.DDM.Psm.Dofs;
using MGroup.Solvers.DDM.Psm.StiffnessDistribution;
using MGroup.Tests.DDM.UnitTests.Models;
using Xunit;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Providers;
using MGroup.Solvers.Tests;

namespace MGroup.Tests.DDM.UnitTests.PFetiDP
{
	public static class PFetiDPPreconditionerTests
	{
		[Theory]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus)]
		[InlineData(EnvironmentChoice.ManagedParSubSingleClus)]
		public static void TestPFetiDPPreconditioner(EnvironmentChoice env)
		{
			IProcessingEnvironment environment = env.Create();

			// Create model
			AllDofs.AddStructuralDofs();
			IStructuralModel model = Example4x4Model.CreateModelHomogeneous();
			Cluster[] clusters = Example4x4Model.CreateClusters(model);
			ICornerDofSelection cornerDofs = Example4x4Model.CreateCornerNodes();
			Example4x4Model.OrderDofs(model);

			#region mock these
			var elementMatrixProvider = new ElementStructuralStiffnessProvider();

			// Initialize components
			var dofSeparatorPsm = new PsmDofSeparator(environment, model, clusters);
			var dofSeparatorFetiDP = new FetiDPDofSeparator(environment, model, clusters);
			var dofSeparatorPFetiDP = new PFetiDPDofSeparator(environment, model, clusters, dofSeparatorPsm, dofSeparatorFetiDP);
			var stiffnessDistribution = new HomogeneousStiffnessDistribution(environment, clusters, dofSeparatorPsm);
			var (matrixManagerBasic, matrixManagerPsm, matrixManagerFetiDP) =
				new PFetiDPMatrixManagerFactoryDense().CreateMatrixManagers(model, dofSeparatorPsm, dofSeparatorFetiDP);
			IFetiDPCoarseProblem coarseProblem = new FetiDPCoarseProblemDense.Factory().Create(environment, model);

			// Separate dofs and calculate mapping matrices
			dofSeparatorPsm.SeparateSubdomainDofsIntoBoundaryInternal();
			dofSeparatorPsm.MapBoundaryDofsBetweenClusterSubdomains();

			dofSeparatorFetiDP.SeparateCornerRemainderDofs(cornerDofs);
			dofSeparatorFetiDP.OrderGlobalCornerDofs();
			dofSeparatorFetiDP.MapCornerDofs();

			stiffnessDistribution.CalcSubdomainScaling();
			dofSeparatorPFetiDP.MapDofsPsmFetiDP(stiffnessDistribution);

			// Calculate all submatrices and perform static condensations
			foreach (ISubdomain sub in model.Subdomains)
			{
				matrixManagerBasic.BuildKff(sub.ID, sub.FreeDofOrdering, sub.Elements, elementMatrixProvider);
				matrixManagerPsm.ExtractKiiKbbKib(sub.ID);
				matrixManagerPsm.InvertKii(sub.ID);
				matrixManagerFetiDP.ExtractKrrKccKrc(sub.ID);
				matrixManagerFetiDP.InvertKrr(sub.ID);
				matrixManagerFetiDP.CalcSchurComplementOfRemainderDofs(sub.ID);
			}

			// Calculate coarse problem matrix
			var mappingsLc = new Dictionary<int, BooleanMatrixRowsToColumns>();
			var matricesKccStar = new Dictionary<int, IMatrix>();
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				int s = subdomain.ID;

				mappingsLc[s] = dofSeparatorFetiDP.GetDofMappingCornerGlobalToSubdomain(s);
				matricesKccStar[s] = matrixManagerFetiDP.GetSchurComplementOfRemainderDofs(s);
			}
			coarseProblem.CreateAndInvertCoarseProblemMatrix(mappingsLc, matricesKccStar);
			#endregion

			var preconditioner = new PFetiDPPreconditioner(environment, model, clusters, dofSeparatorPsm, dofSeparatorFetiDP,
						dofSeparatorPFetiDP, matrixManagerFetiDP, coarseProblem);

			// Check
			Matrix expectedPreconditioner = Example4x4ExpectedResults.GetPreconditioner();
			Matrix computedPreconditioner = Utilities.CreateExplicitMatrix(
				expectedPreconditioner.NumRows,
				expectedPreconditioner.NumColumns,
				x =>
				{
					var y = Vector.CreateZero(x.Length);
					preconditioner.SolveLinearSystem(x, y);
					return y;
				});

			Assert.True(expectedPreconditioner.Equals(computedPreconditioner));
		}
	}
}
