using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using MGroup.Solvers.DDM;
using MGroup.Solvers.DDM.Dofs;
using MGroup.Solvers.DDM.Environments;
using MGroup.Solvers.DDM.FetiDP.Dofs;
using MGroup.Solvers.Tests;
using MGroup.Tests.DDM.UnitTests.Models;
using Xunit;

namespace MGroup.Tests.DDM.UnitTests.FetiDP
{
	public static class FetiDPDofSeparatorTests
	{
		[Theory]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus)]
		[InlineData(EnvironmentChoice.ManagedParSubSingleClus)]
		public static void TestDofSeparation(EnvironmentChoice env)
		{
			IDdmEnvironment environment = env.Create();

			// Create model
			AllDofs.AddStructuralDofs();
			IStructuralModel model = Example4x4Model.CreateModelHomogeneous();
			Cluster[] clusters = Example4x4Model.CreateClusters(model);
			ICornerDofSelection cornerDofs = Example4x4Model.CreateCornerNodes();
			Example4x4Model.OrderDofs(model);

			// Separate dofs
			var dofSeparator = new FetiDPDofSeparator(environment, model, clusters);
			dofSeparator.SeparateCornerRemainderDofs(cornerDofs);

			// Check
			foreach (ISubdomain sub in model.Subdomains)
			{
				(int[] cornerDofsExpected, int[] remainderDofsExpected) = 
					Example4x4ExpectedResults.GetDofsCornerRemainderToFree(sub.ID);
				int[] cornerDofsComputed = dofSeparator.GetDofsCornerToFree(sub.ID);
				int[] remainderDofsComputed = dofSeparator.GetDofsRemainderToFree(sub.ID);
				Utilities.AssertEqual(cornerDofsExpected, cornerDofsComputed);
				Utilities.AssertEqual(remainderDofsExpected, remainderDofsComputed);
			}
		}

		[Theory]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus)]
		[InlineData(EnvironmentChoice.ManagedParSubSingleClus)]
		public static void TestCornerMappingMatrices(EnvironmentChoice env)
		{
			IDdmEnvironment environment = env.Create();

			// Create model
			AllDofs.AddStructuralDofs();
			IStructuralModel model = Example4x4Model.CreateModelHomogeneous();
			Cluster[] clusters = Example4x4Model.CreateClusters(model);
			ICornerDofSelection cornerDofs = Example4x4Model.CreateCornerNodes();
			Example4x4Model.OrderDofs(model);

			// Separate dofs and calculate the boolean matrices
			var dofSeparator = new FetiDPDofSeparator(environment, model, clusters);
			dofSeparator.SeparateCornerRemainderDofs(cornerDofs);
			dofSeparator.OrderGlobalCornerDofs();
			dofSeparator.MapCornerDofs();

			// Check
			foreach (ISubdomain sub in model.Subdomains)
			{
				Matrix computedLc = dofSeparator.GetDofMappingCornerGlobalToSubdomain(sub.ID).CopyToFullMatrix();
				Matrix expectedLc = Example4x4ExpectedResults.GetMatrixLc(sub.ID);
				Assert.True(expectedLc.Equals(computedLc));
			}
		}
	}
}
