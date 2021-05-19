using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.Solvers.Ordering;
using ISAAR.MSolve.Solvers.Ordering.Reordering;
using MGroup.Solvers_OLD;
using MGroup.Solvers_OLD.DDM.Environments;
using MGroup.Solvers_OLD.DDM.Mappings;
using MGroup.Solvers_OLD.DDM.Psm.Dofs;
using MGroup.Solvers_OLD.DofOrdering;
using MGroup.Solvers_OLD.DofOrdering.Reordering;
using Xunit;

namespace MGroup.Tests.DDM.UnitTests.Psm
{
	public class DofSeparatorTestsOLD
	{
		[Fact]
		public static void TestDofSeparation()
		{
			(IStructuralModel model, IPsmDofSeparator dofSeparator) = CreateModelAndDofSeparator();

			// Check
			foreach (ISubdomain sub in model.Subdomains)
			{
				int[] boundaryDofsExpected = Example4x4QuadsHomogeneous.GetDofsBoundary(sub.ID);
				int[] boundaryDofsComputed = dofSeparator.GetSubdomainDofsBoundaryToFree(sub.ID);
				int[] internalDofsExpected = Example4x4QuadsHomogeneous.GetDofsInternal(sub.ID);
				int[] internalDofsComputed = dofSeparator.GetSubdomainDofsInternalToFree(sub.ID);
				CheckEqual(boundaryDofsExpected, boundaryDofsComputed);
				CheckEqual(internalDofsExpected, internalDofsComputed);
			}
		}

		[Fact]
		public static void TestBoundaryMapMatrices()
		{
			(IStructuralModel model, IPsmDofSeparator dofSeparator) = CreateModelAndDofSeparator();

			// Check
			foreach (ISubdomain sub in model.Subdomains)
			{
				IMappingMatrix computedLb = dofSeparator.GetDofMappingBoundaryClusterToSubdomain(sub.ID);
				double[,] expectedLb = Example4x4QuadsHomogeneous.GetMatrixLb(sub.ID);
				Assert.True(Matrix.CreateFromArray(expectedLb).Equals(computedLb.CopyToFullMatrix()));
			}
		}

		public static (IStructuralModel, IPsmDofSeparator) CreateModelAndDofSeparator()
		{
			// Create model
			AllDofs.AddStructuralDofs();
			Model model = Example4x4QuadsHomogeneous.CreateModel();
			model.ConnectDataStructures();

			var clusters = new Solvers_OLD.DDM.Cluster[1];
			clusters[0] = new Solvers_OLD.DDM.Cluster(0);
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				clusters[0].Subdomains.Add(subdomain);
			}

			// Order free dofs.
			var dofOrderer = new ReusingDofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());
			IGlobalFreeDofOrdering globalOrdering = dofOrderer.OrderFreeDofs(model);
			model.GlobalDofOrdering = globalOrdering;
			foreach (ISubdomain sub in model.Subdomains)
			{
				sub.FreeDofOrdering = globalOrdering.SubdomainDofOrderings[sub];
			}

			// Separate dofs and calculate the boolean matrices
			IDdmEnvironment environment = EnvironmentChoice.ManagedSeqSubSingleClus.Create();
			var dofSeparator = new PsmDofSeparator(environment, model, clusters);
			dofSeparator.SeparateSubdomainDofsIntoBoundaryInternal();
			dofSeparator.MapBoundaryDofsBetweenClusterSubdomains();
			return (model, dofSeparator);
		}

		internal static void CheckEqual(int[] expected, int[] computed)
		{
			Assert.Equal(expected.Length, computed.Length);
			for (int i = 0; i < expected.Length; ++i) Assert.Equal(expected[i], computed[i]);
		}
	}
}
