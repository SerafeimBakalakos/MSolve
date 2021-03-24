using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Providers;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using MGroup.Solvers.DDM;
using MGroup.Solvers.DDM.Dofs;
using MGroup.Solvers.DDM.Environments;
using MGroup.Solvers.DDM.FetiDP.Dofs;
using MGroup.Solvers.DDM.Mappings;
using MGroup.Solvers.DDM.PFetiDP.Dofs;
using MGroup.Solvers.DDM.Psm.Dofs;
using MGroup.Solvers.DDM.Psm.StiffnessDistribution;
using MGroup.Solvers.DDM.Psm.StiffnessMatrices;
using MGroup.Solvers.DDM.StiffnessMatrices;
using MGroup.Tests.DDM.UnitTests.Models;
using Xunit;

namespace MGroup.Tests.DDM.UnitTests.PFetiDP
{
	public static class PFetiDPDofSeparatorTests
	{
		[Theory]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus)]
		[InlineData(EnvironmentChoice.ManagedParSubSingleClus)]
		public static void TestCornerMappingMatricesHomogeneous(EnvironmentChoice env)
		{
			IProcessingEnvironment environment = env.Create();

			// Create model
			AllDofs.AddStructuralDofs();
			IStructuralModel model = Example4x4Model.CreateModelHomogeneous();
			Cluster[] clusters = Example4x4Model.CreateClusters(model);
			ICornerDofSelection cornerDofs = Example4x4Model.CreateCornerNodes();
			Example4x4Model.OrderDofs(model);

			#region mock these
			// Separate dofs and calculate the boolean matrices
			var dofSeparatorPsm = new PsmDofSeparator(environment, model, clusters);
			dofSeparatorPsm.SeparateBoundaryInternalDofs();
			dofSeparatorPsm.MapBoundaryDofs();

			var dofSeparatorFetiDP = new FetiDPDofSeparator(environment, model, clusters);
			dofSeparatorFetiDP.SeparateCornerRemainderDofs(cornerDofs);
			dofSeparatorFetiDP.OrderGlobalCornerDofs();
			dofSeparatorFetiDP.MapCornerDofs();

			var stiffnessDistribution = new HomogeneousStiffnessDistribution(environment, clusters, dofSeparatorPsm);
			stiffnessDistribution.CalcSubdomainScaling();
			#endregion

			var dofSeparatorPFetiDP = new PFetiDPDofSeparator(environment, model, clusters, dofSeparatorPsm, dofSeparatorFetiDP);
			dofSeparatorPFetiDP.MapDofsPsmFetiDP(stiffnessDistribution);

			// Check
			foreach (ISubdomain sub in model.Subdomains)
			{
				Matrix computedLpr = 
					dofSeparatorPFetiDP.GetDofMappingBoundaryClusterToSubdomainRemainder(sub.ID).CopyToFullMatrix();
				Matrix expectedLpr = Example4x4ExpectedResults.GetMatrixLprHomogeneous(sub.ID);
				Assert.True(expectedLpr.Equals(computedLpr));
			}

			#region debug
			try
			{
				Matrix computedNcb =
					dofSeparatorPFetiDP.GetDofMappingGlobalCornerToClusterBoundary(clusters[0].ID).CopyToFullMatrix();
				Matrix expectedNcb = Example4x4ExpectedResults.GetMatrixNcb();
				Assert.True(expectedNcb.Equals(computedNcb));
			}
			catch (Exception ex)
			{
				string path = @"C:\Users\Serafeim\Desktop\PFETIDP\matrices\bNc.txt";
				var writer = new ISAAR.MSolve.LinearAlgebra.Output.FullMatrixWriter();
				writer.NumericFormat = new ISAAR.MSolve.LinearAlgebra.Output.Formatting.GeneralNumericFormat();
				Matrix computedNcb =
					dofSeparatorPFetiDP.GetDofMappingGlobalCornerToClusterBoundary(clusters[0].ID).CopyToFullMatrix();
				writer.WriteToFile(computedNcb, path);
				throw ex;
			}
			#endregion
		}

		[Theory]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus)]
		[InlineData(EnvironmentChoice.ManagedParSubSingleClus)]
		public static void TestCornerMappingMatricesHeterogeneous(EnvironmentChoice env)
		{
			IProcessingEnvironment environment = env.Create();

			// Create model
			AllDofs.AddStructuralDofs();
			IStructuralModel model = Example4x4Model.CreateModelHomogeneous();
			Cluster[] clusters = Example4x4Model.CreateClusters(model);
			ICornerDofSelection cornerDofs = Example4x4Model.CreateCornerNodes();
			Example4x4Model.OrderDofs(model);

			#region mock these
			// Separate dofs and calculate the boolean matrices
			var dofSeparatorPsm = new PsmDofSeparator(environment, model, clusters);
			var dofSeparatorFetiDP = new FetiDPDofSeparator(environment, model, clusters);

			dofSeparatorPsm.SeparateBoundaryInternalDofs();
			dofSeparatorPsm.MapBoundaryDofs();

			dofSeparatorFetiDP.SeparateCornerRemainderDofs(cornerDofs);
			dofSeparatorFetiDP.OrderGlobalCornerDofs();
			dofSeparatorFetiDP.MapCornerDofs();

			var factory = new PsmMatrixManagerDense.Factory();
			var (matrixManagerBasic, matrixManagerPsm) = factory.CreateMatrixManagers(model, dofSeparatorPsm);
			var elementMatrixProvider = new ElementStructuralStiffnessProvider();
			foreach (ISubdomain sub in model.Subdomains)
			{
				matrixManagerBasic.BuildKff(sub.ID, sub.FreeDofOrdering, sub.Elements, elementMatrixProvider);
			}

			var stiffnessDistribution = new HeterogeneousStiffnessDistribution(
				environment, clusters, dofSeparatorPsm, matrixManagerBasic);
			stiffnessDistribution.CalcSubdomainScaling();
			#endregion

			var dofSeparatorPFetiDP = new PFetiDPDofSeparator(environment, model, clusters, dofSeparatorPsm, dofSeparatorFetiDP);
			dofSeparatorPFetiDP.MapDofsPsmFetiDP(stiffnessDistribution);

			// Check
			foreach (ISubdomain sub in model.Subdomains)
			{
				Matrix computedLpr =
					dofSeparatorPFetiDP.GetDofMappingBoundaryClusterToSubdomainRemainder(sub.ID).CopyToFullMatrix();
				Matrix expectedLpr = Example4x4ExpectedResults.GetMatrixLprHeterogeneous(sub.ID);
				Assert.True(expectedLpr.Equals(computedLpr));
			}

			Matrix computedNcb =
				dofSeparatorPFetiDP.GetDofMappingGlobalCornerToClusterBoundary(clusters[0].ID).CopyToFullMatrix();
			Matrix expectedNcb = Example4x4ExpectedResults.GetMatrixNcb();
			Assert.True(expectedNcb.Equals(computedNcb));
		}
	}
}
