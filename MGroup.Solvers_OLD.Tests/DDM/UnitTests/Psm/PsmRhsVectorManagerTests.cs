using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using MGroup.Solvers_OLD.DDM;
using MGroup.Solvers_OLD;
using MGroup.Solvers_OLD.DDM.Psm.Dofs;
using MGroup.Tests.DDM.UnitTests.Models;
using MGroup.Solvers_OLD.DDM.Psm.StiffnessMatrices;
using MGroup.Solvers_OLD.DDM.StiffnessMatrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers_OLD.DDM.Psm.Vectors;
using MGroup.Solvers_OLD.DDM.Environments;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Solvers.LinearSystems;
using ISAAR.MSolve.Discretization.Providers;

namespace MGroup.Tests.DDM.UnitTests.Psm
{
	public static class PsmRhsVectorManagerTests
	{
		[Theory]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus)]
		[InlineData(EnvironmentChoice.ManagedParSubSingleClus)]
		public static void TestSubdomainRhsVectors(EnvironmentChoice env)
		{
			IDdmEnvironment environment = env.Create();
			(IStructuralModel model, IPsmDofSeparator dofSeparator, IPsmMatrixManager matrixManager, 
				Dictionary<int, ILinearSystem> linearSystems) = PrepareTest(environment);
			var rhsManager = new PsmRhsVectorManager(environment, model, linearSystems, dofSeparator, matrixManager);

			// Calculate all rhs vectors
			rhsManager.CalcRhsVectors();

			// Check
			foreach (ISubdomain sub in model.Subdomains)
			{
				int s = sub.ID;
				Vector computedFi = rhsManager.GetInternalRhs(s);
				Vector computedFbCondensed = rhsManager.GetBoundaryCondensedRhs(s);
				Vector expectedFi = Example4x4ExpectedResults.GetRhsFiHomogeneous(s);
				Vector expectedFbCondensed = Example4x4ExpectedResults.GetRhsFbHatHomogeneous(s);

				Assert.True(expectedFi.Equals(computedFi));
				Assert.True(expectedFbCondensed.Equals(computedFbCondensed));
			}
		}

		[Theory]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus)]
		[InlineData(EnvironmentChoice.ManagedParSubSingleClus)]
		public static void TestInterfaceProblemRhsVector(EnvironmentChoice env)
		{
			IDdmEnvironment environment = env.Create();
			(IStructuralModel model, IPsmDofSeparator dofSeparator, IPsmMatrixManager matrixManager,
				Dictionary<int, ILinearSystem> linearSystems) = PrepareTest(environment);
			var rhsManager = new PsmRhsVectorManager(environment, model, linearSystems, dofSeparator, matrixManager);

			// Calculate all rhs vectors
			rhsManager.CalcRhsVectors();

			// Check
			Vector computedFbGlobal = rhsManager.InterfaceProblemRhs;
			Vector expectedFbGlobal = Example4x4ExpectedResults.GetRhsInterfaceProblem();
			Assert.True(expectedFbGlobal.Equals(computedFbGlobal));
		}

		private static (IStructuralModel, IPsmDofSeparator, IPsmMatrixManager, Dictionary<int, ILinearSystem>) PrepareTest(
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

			// Stiffness matrices
			var elementMatrixProvider = new ElementStructuralStiffnessProvider();
			IPsmMatrixManagerFactory matrixManagerFactory = new PsmMatrixManagerDense.Factory();
			(IMatrixManager matrixManagerBasic, IPsmMatrixManager matrixManagerPsm)
				= matrixManagerFactory.CreateMatrixManagers(model, dofSeparator);
			foreach (ISubdomain sub in model.Subdomains)
			{
				matrixManagerBasic.BuildKff(sub.ID, sub.FreeDofOrdering, sub.Elements, elementMatrixProvider);
				matrixManagerPsm.ExtractKiiKbbKib(sub.ID);
				matrixManagerPsm.InvertKii(sub.ID);
			}

			// Create Ff vectors
			var linearSystems = new Dictionary<int, ILinearSystem>();
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				int s = subdomain.ID;
				linearSystems[s] = matrixManagerBasic.GetLinearSystem(s);
				linearSystems[s].Reset();
				Vector Ff = Example4x4ExpectedResults.GetRhsFfHomogeneous(s);
				linearSystems[s].RhsVector = Ff;
			}
			#endregion

			return (model, dofSeparator, matrixManagerPsm, linearSystems);
		}
	}
}
