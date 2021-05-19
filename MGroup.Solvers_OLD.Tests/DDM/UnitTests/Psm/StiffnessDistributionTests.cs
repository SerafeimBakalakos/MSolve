using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Providers;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using MGroup.Solvers_OLD;
using MGroup.Solvers_OLD.DDM;
using MGroup.Solvers_OLD.DDM.Environments;
using MGroup.Solvers_OLD.DDM.Psm.Dofs;
using MGroup.Solvers_OLD.DDM.Psm.StiffnessDistribution;
using MGroup.Solvers_OLD.DDM.Psm.StiffnessMatrices;
using MGroup.Tests.DDM.UnitTests.Models;
using Xunit;

namespace MGroup.Tests.DDM.UnitTests.Psm
{
	public static class StiffnessDistributionTests
	{
		[Theory]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus)]
		[InlineData(EnvironmentChoice.ManagedParSubSingleClus)]
		public static void TestMappingMatricesHomogeneous(EnvironmentChoice env)
		{
			IDdmEnvironment environment = env.Create();

			// Create model
			AllDofs.AddStructuralDofs();
			IStructuralModel model = Example4x4Model.CreateModelHomogeneous();
			Cluster[] clusters = Example4x4Model.CreateClusters(model);
			Example4x4Model.OrderDofs(model);

			#region mock these
			// Dof separation
			var dofSeparator = new PsmDofSeparator(environment, model, clusters);
			dofSeparator.SeparateSubdomainDofsIntoBoundaryInternal();
			dofSeparator.MapBoundaryDofsBetweenClusterSubdomains();
			#endregion

			// Stiffness distribution
			var stiffnessDistribution = new HomogeneousStiffnessDistribution(environment, clusters, dofSeparator);
			stiffnessDistribution.CalcSubdomainScaling();

			// Check
			foreach (ISubdomain sub in model.Subdomains)
			{
				Matrix computedLpb = stiffnessDistribution.GetDofMappingBoundaryClusterToSubdomain(sub.ID).CopyToFullMatrix();
				Matrix expectedLpb = Example4x4ExpectedResults.GetMatrixLpbHomogeneous(sub.ID);
				Assert.True(expectedLpb.Equals(computedLpb));
			}
		}

		[Theory]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus)]
		[InlineData(EnvironmentChoice.ManagedParSubSingleClus)]
		public static void TestMappingMatricesHeterogeneous(EnvironmentChoice env)
		{
			IDdmEnvironment environment = env.Create();

			// Create model
			AllDofs.AddStructuralDofs();
			IStructuralModel model = Example4x4Model.CreateModelHomogeneous();
			Cluster[] clusters = Example4x4Model.CreateClusters(model);
			Example4x4Model.OrderDofs(model);

			#region mock these
			// Dof separation
			var dofSeparator = new PsmDofSeparator(environment, model, clusters);
			dofSeparator.SeparateSubdomainDofsIntoBoundaryInternal();
			dofSeparator.MapBoundaryDofsBetweenClusterSubdomains();

			var factory = new PsmMatrixManagerDense.Factory();
			var (matrixManagerBasic, matrixManagerPsm) = factory.CreateMatrixManagers(model, dofSeparator);
			var elementMatrixProvider = new ElementStructuralStiffnessProvider();
			foreach (ISubdomain sub in model.Subdomains)
			{
				matrixManagerBasic.BuildKff(sub.ID, sub.FreeDofOrdering, sub.Elements, elementMatrixProvider);
			}
			#endregion

			// Stiffness distribution
			var stiffnessDistribution = new HeterogeneousStiffnessDistribution(
				environment, clusters, dofSeparator, matrixManagerBasic);
			stiffnessDistribution.CalcSubdomainScaling();

			// Check
			foreach (ISubdomain sub in model.Subdomains)
			{
				Matrix computedLpb = stiffnessDistribution.GetDofMappingBoundaryClusterToSubdomain(sub.ID).CopyToFullMatrix();
				Matrix expectedLpb = Example4x4ExpectedResults.GetMatrixLpbHeterogeneous(sub.ID);
				Assert.True(expectedLpb.Equals(computedLpb));
			}
		}
	}
}
