using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Solvers.Ordering;
using ISAAR.MSolve.Solvers.Ordering.Reordering;
using MGroup.Solvers_OLD.DDM;
using MGroup.Solvers_OLD.DDM.FetiDP.Dofs;
using MGroup.Solvers_OLD.DofOrdering;
using MGroup.Solvers_OLD.DofOrdering.Reordering;

namespace MGroup.Tests.DDM.UnitTests.Models
{
	public class Example4x4Model
	{

		//										 Λ P
		// |> 4 ----- 9 ----- 14 ---- 19 ---- 24 | 
		//    |  (e3) |  (e7) | (e11) | (e15) |
		//    |       |       |       |       |  Λ P
		// |> 3 ----- 8 ----- 13 ---- 18 ---- 23 | 
		//    |  (e2) |  (e6) | (e10) | (e14) |
		//    |       |       |       |       |  Λ P
		// |> 2 ----- 7 ----- 12 ---- 17 ---- 22 | 
		//    |  (e1) |  (e5) |  (e9) | (e13) |
		//    |       |       |       |       |  Λ P
		// |> 1 ----- 6 ----- 11 ---- 16 ---- 21 | 
		//    |  (e0) |  (e4) |  (e8) | (e12) |
		//    |       |       |       |       |  Λ P
		// |> 0 ----- 5 ----- 10 ---- 15 ---- 20 | 

		// subdomain 1				subdomain 3
		// |> 4 ----- 9 ----- 14	14 ---- 19 ---- 24
		//    |  (e3) |  (e7) | 	| (e11) | (e15) |
		//    |       |       | 	|       |       |
		// |> 3 ----- 8 ----- 13	13 ---- 18 ---- 23
		//    |  (e2) |  (e6) | 	| (e10) | (e14) |
		//    |       |       | 	|       |       |
		// |> 2 ----- 7 ----- 12	12 ---- 17 ---- 22


		// subdomain 0				subdomain 2
		// |> 2 ----- 7 ----- 12	12 ---- 17 ---- 22
		//    |  (e1) |  (e5) | 	|  (e9) | (e13) |
		//    |       |       | 	|       |       |
		// |> 1 ----- 6 ----- 11	11 ---- 16 ---- 21
		//    |  (e0) |  (e4) | 	|  (e8) | (e12) |
		//    |       |       | 	|       |       |
		// |> 0 ----- 5 ----- 10	10 ---- 15 ---- 20

		public static IStructuralModel CreateModelHomogeneous()
		{
			double E0 = 2.1E7;

			var builder = new Uniform2DModelBuilderYMajor();
			builder.DomainLengthX = 4.0;
			builder.DomainLengthY = 4.0;
			builder.NumSubdomainsX = 2;
			builder.NumSubdomainsY = 2;
			builder.NumTotalElementsX = 4;
			builder.NumTotalElementsY = 4;
			builder.YoungModulus = E0;
			builder.PrescribeDisplacement(Uniform2DModelBuilderYMajor.BoundaryRegion.LeftSide, StructuralDof.TranslationX, 0.0);
			builder.PrescribeDisplacement(Uniform2DModelBuilderYMajor.BoundaryRegion.LeftSide, StructuralDof.TranslationY, 0.0);
			builder.DistributeLoadAtNodes(Uniform2DModelBuilderYMajor.BoundaryRegion.RightSide, StructuralDof.TranslationY, 100.0);

			IStructuralModel model = builder.BuildModel();
			model.ConnectDataStructures();
			return model;
		}

		public static Cluster[] CreateClusters(IStructuralModel model)
		{
			var clusters = new Solvers_OLD.DDM.Cluster[1];
			clusters[0] = new Solvers_OLD.DDM.Cluster(0);
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				clusters[0].Subdomains.Add(subdomain);
			}
			return clusters;
		}

		public static ICornerDofSelection CreateCornerNodes()
		{
			var cornerDofs = new UserDefinedCornerDofSelection();
			cornerDofs.AddCornerNode(10);
			cornerDofs.AddCornerNode(12);
			cornerDofs.AddCornerNode(14);
			cornerDofs.AddCornerNode(22);
			return cornerDofs;
		}

		public static void OrderDofs(IStructuralModel model)
		{
			var dofOrderer = new ReusingDofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());
			IGlobalFreeDofOrdering globalOrdering = dofOrderer.OrderFreeDofs(model);
			model.GlobalDofOrdering = globalOrdering;
			foreach (ISubdomain sub in model.Subdomains)
			{
				sub.FreeDofOrdering = globalOrdering.SubdomainDofOrderings[sub];
			}
		}
	}
}
