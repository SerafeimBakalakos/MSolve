using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using MGroup.Solvers.DDM;
using MGroup.Solvers.DDM.Dofs;
using MGroup.Solvers.DDM.Environments;
using MGroup.Solvers.DDM.Psm.Dofs;
using MGroup.Solvers.Tests;
using MGroup.Tests.DDM.UnitTests.Models;
using Xunit;

namespace MGroup.Tests.DDM.UnitTests.Psm
{
	public static class PsmDofSeparatorTests
	{
		[Theory]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus)]
		[InlineData(EnvironmentChoice.ManagedParSubSingleClus)]
		public static void TestDofSeparation(EnvironmentChoice env)
		{
			// Create model
			AllDofs.AddStructuralDofs();
			IStructuralModel model = Example4x4Model.CreateModelHomogeneous();
			Cluster[] clusters = Example4x4Model.CreateClusters(model);
			Example4x4Model.OrderDofs(model);

			// Separate dofs
			IDdmEnvironment environment = env.Create();
			var dofSeparator = new PsmDofSeparator(environment, model, clusters);
			dofSeparator.SeparateSubdomainDofsIntoBoundaryInternal();

			// Check
			foreach (ISubdomain sub in model.Subdomains)
			{
				(int[] boundaryDofsExpected, int[] internalDofsExpected) = 
					Example4x4ExpectedResults.GetDofsBoundaryInternalToFree(sub.ID);
				int[] boundaryDofsComputed = dofSeparator.GetSubdomainDofsBoundaryToFree(sub.ID);
				int[] internalDofsComputed = dofSeparator.GetSubdomainDofsInternalToFree(sub.ID);
				Utilities.AssertEqual(boundaryDofsExpected, boundaryDofsComputed);
				Utilities.AssertEqual(internalDofsExpected, internalDofsComputed);
			}
		}

		[Theory]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus)]
		[InlineData(EnvironmentChoice.ManagedParSubSingleClus)]
		public static void TestBoundaryMappingMatrices(EnvironmentChoice env)
		{
			// Create model
			AllDofs.AddStructuralDofs();
			IStructuralModel model = Example4x4Model.CreateModelHomogeneous();
			Cluster[] clusters = Example4x4Model.CreateClusters(model);
			Example4x4Model.OrderDofs(model);

			// Separate dofs and calculate the boolean matrices
			IDdmEnvironment environment = env.Create();
			var dofSeparator = new PsmDofSeparator(environment, model, clusters);
			dofSeparator.SeparateSubdomainDofsIntoBoundaryInternal();
			dofSeparator.MapBoundaryDofsBetweenClusterSubdomains();

			// Check
			foreach (ISubdomain sub in model.Subdomains)
			{
				Matrix computedLb = dofSeparator.GetDofMappingBoundaryClusterToSubdomain(sub.ID).CopyToFullMatrix();
				Matrix expectedLb = Example4x4ExpectedResults.GetMatrixLb(sub.ID);
				Assert.True(expectedLb.Equals(computedLb));
			}
		}
	}
}
