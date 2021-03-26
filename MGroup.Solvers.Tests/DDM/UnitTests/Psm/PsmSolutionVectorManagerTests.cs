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
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.DDM.Psm.Vectors;
using MGroup.Solvers.DDM.Environments;
using ISAAR.MSolve.Solvers.LinearSystems;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Providers;

namespace MGroup.Tests.DDM.UnitTests.Psm
{
	public static class PsmSolutionVectorManagerTests
	{
		[Theory]
		[InlineData(EnvironmentChoice.ManagedSeqSubSingleClus)]
		[InlineData(EnvironmentChoice.ManagedParSubSingleClus)]
		public static void TestSubdomainUfVectors(EnvironmentChoice env)
		{
			IProcessingEnvironment environment = env.Create();
			(IStructuralModel model, IPsmDofSeparator dofSeparator, IMatrixManager matrixManagerBasic, IPsmMatrixManager matrixManagerPsm,
				Dictionary<int, ILinearSystem> linearSystems, IPsmRhsVectorManager rhsManager) = PrepareTest(environment);
			var solutionVectorManager = new PsmSolutionVectorManager(environment, model, linearSystems, dofSeparator,
				 matrixManagerBasic, matrixManagerPsm, rhsManager);

			// Setup global displacements
			solutionVectorManager.Initialize();
			solutionVectorManager.GlobalBoundaryDisplacements.CopyFrom(Example4x4ExpectedResults.GetSolutionInterfaceProblem());

			// Calculate subdomain displacements
			solutionVectorManager.CalcSubdomainDisplacements();

			// Check
			foreach (ISubdomain sub in model.Subdomains)
			{
				int s = sub.ID;
				IVectorView computedUf = linearSystems[s].Solution;
				Vector expectedUf = Example4x4ExpectedResults.GetSolutionUf(s);
				Assert.True(expectedUf.Equals(computedUf));
			}
		}

		private static (IStructuralModel, IPsmDofSeparator, IMatrixManager, IPsmMatrixManager, Dictionary<int, ILinearSystem>,
			IPsmRhsVectorManager) PrepareTest(IProcessingEnvironment environment)
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

			var linearSystems = new Dictionary<int, ILinearSystem>();
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				int s = subdomain.ID;
				linearSystems[s] = matrixManagerBasic.GetLinearSystem(s);
				linearSystems[s].Reset();
				Vector Ff = Example4x4ExpectedResults.GetRhsFfHomogeneous(s);
				linearSystems[s].RhsVector = Ff;
			}

			var rhsManager = new PsmRhsVectorManager(environment, model, linearSystems, dofSeparator, matrixManagerPsm);
			rhsManager.CalcRhsVectors();
			#endregion

			return (model, dofSeparator, matrixManagerBasic, matrixManagerPsm, linearSystems, rhsManager);
		}
	}
}
