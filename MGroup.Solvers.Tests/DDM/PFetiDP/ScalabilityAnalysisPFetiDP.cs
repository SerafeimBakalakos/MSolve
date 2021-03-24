using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Iterative.PreconditionedConjugateGradient;
using ISAAR.MSolve.LinearAlgebra.Iterative.Termination;
using ISAAR.MSolve.Solvers;
using MGroup.Solvers.DDM;
using MGroup.Solvers.DDM.FetiDP.CoarseProblem;
using MGroup.Solvers.DDM.FetiDP.Dofs;
using MGroup.Solvers.DDM.PFetiDP;
using MGroup.Solvers.DDM.PFetiDP.StiffnessMatrices;
using MGroup.Solvers.DDM.Psm.InterfaceProblem;
using MGroup.Tests.DDM.ScalabilityAnalysis;
using Xunit;

namespace MGroup.Tests.DDM.PFetiDP
{
	public class ScalabilityAnalysisPFetiDP : ScalabilityAnalysisBase
	{
		//[Fact]
		public static void RunFullScalabilityAnalysisCantilever2D()
		{
			string outputDirectory = @"C:\Users\Serafeim\Desktop\PFETIDP\results\cantilever2D\";
			var scalabilityAnalysis = new ScalabilityAnalysisPFetiDP();
			scalabilityAnalysis.ModelBuilder = new CantilevelBeam2D();
			scalabilityAnalysis.EnableNativeDlls = true;
			scalabilityAnalysis.IterativeResidualTolerance = 1E-6;

			scalabilityAnalysis.RunParametricConstNumSubdomains(outputDirectory);
			scalabilityAnalysis.RunParametricConstNumElements(outputDirectory);
			scalabilityAnalysis.RunParametricConstSubdomainPerElementSize(outputDirectory);
		}

		//[Fact]
		public static void RunFullScalabilityAnalysisCantilever3D()
		{
			string outputDirectory = @"C:\Users\Serafeim\Desktop\PFETIDP\results\cantilever3D\";
			var scalabilityAnalysis = new ScalabilityAnalysisPFetiDP();
			scalabilityAnalysis.ModelBuilder = new CantilevelBeam3D();
			scalabilityAnalysis.EnableNativeDlls = true;
			scalabilityAnalysis.IterativeResidualTolerance = 1E-6;

			scalabilityAnalysis.RunParametricConstNumSubdomains(outputDirectory);
			scalabilityAnalysis.RunParametricConstNumElements(outputDirectory);
			scalabilityAnalysis.RunParametricConstSubdomainPerElementSize(outputDirectory);
		}

		//[Fact]
		public static void RunFullScalabilityAnalysisRve2D()
		{
			string outputDirectory = @"C:\Users\Serafeim\Desktop\PFETIDP\results\rve2D\";
			var scalabilityAnalysis = new ScalabilityAnalysisPFetiDP();
			scalabilityAnalysis.ModelBuilder = new Rve2D();
			scalabilityAnalysis.EnableNativeDlls = true;
			scalabilityAnalysis.IterativeResidualTolerance = 1E-6;

			scalabilityAnalysis.RunParametricConstNumSubdomains(outputDirectory);
			scalabilityAnalysis.RunParametricConstNumElements(outputDirectory);
			scalabilityAnalysis.RunParametricConstSubdomainPerElementSize(outputDirectory);
		}

		//[Fact]
		public static void RunFullScalabilityAnalysisRve3D()
		{
			string outputDirectory = @"C:\Users\Serafeim\Desktop\PFETIDP\results\rve3D\";
			var scalabilityAnalysis = new ScalabilityAnalysisPFetiDP();
			scalabilityAnalysis.ModelBuilder = new Rve3D();
			scalabilityAnalysis.EnableNativeDlls = true;
			scalabilityAnalysis.IterativeResidualTolerance = 1E-6;

			scalabilityAnalysis.RunParametricConstNumSubdomains(outputDirectory);
			scalabilityAnalysis.RunParametricConstNumElements(outputDirectory);
			scalabilityAnalysis.RunParametricConstSubdomainPerElementSize(outputDirectory);
		}

		public override ISolver CreateSolver(IStructuralModel model)
		{
			var clusters = new Cluster[1];
			clusters[0] = new Cluster(0);
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				clusters[0].Subdomains.Add(subdomain);
			}

			var cornerDofs = new UserDefinedCornerDofSelection();
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				var corners = CornerNodeUtilities.FindCornersOfRectangle2D(subdomain);
				foreach (INode node in corners)
				{
					if (node.GetMultiplicity() > 1)
					{
						cornerDofs.AddCornerNode(node.ID);
					}
				}
			}

			// Solver
			var solverBuilder = new PFetiDPSolver.Builder();

			// Homegeneous/heterogeneous stiffness distribution
			solverBuilder.IsHomogeneousProblem = true;

			// Specify the format of PSM's matrices
			if (EnableNativeDlls)
			{
				solverBuilder.CoarseProblemFactory = new FetiDPCoarseProblemSuiteSparse.Factory();
				solverBuilder.MatrixManagerFactory = new PFetiDPMatrixManagerFactorySuiteSparse();
			}

			// Specify solver for the interface problem of PSM
			var pcgBuilder = new PcgAlgorithm.Builder();
			pcgBuilder.ResidualTolerance = IterativeResidualTolerance;
			pcgBuilder.MaxIterationsProvider = new PercentageMaxIterationsProvider(1.0);
			solverBuilder.InterfaceProblemSolver = new InterfaceProblemSolverPcg(pcgBuilder.Build());

			return solverBuilder.BuildSolver(model, cornerDofs, clusters);
		}
	}
}
