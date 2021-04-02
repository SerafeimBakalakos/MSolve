namespace MGroup.Tests.DDM.PFetiDP
{
	using System;
    using ISAAR.MSolve.Analyzers;
    using ISAAR.MSolve.Discretization.FreedomDegrees;
    using ISAAR.MSolve.Discretization.Interfaces;
    using ISAAR.MSolve.FEM.Entities;
    using ISAAR.MSolve.LinearAlgebra.Vectors;
    using ISAAR.MSolve.Problems;
    using ISAAR.MSolve.Solvers;
    using ISAAR.MSolve.Solvers.Direct;
    using MGroup.Solvers.DDM;
	using MGroup.Solvers.DDM.Environments;
	using MGroup.Solvers.DDM.FetiDP.CoarseProblem;
	using MGroup.Solvers.DDM.FetiDP.Dofs;
	using MGroup.Solvers.DDM.PFetiDP;
	using MGroup.Solvers.DDM.PFetiDP.StiffnessMatrices;
	using Xunit;

	/// <summary>
	/// Tests from Papagiannakis bachelor thesis (NTUA 2011), p. 134 - 147
	/// Authors: Serafeim Bakalakos
	/// </summary>
	public static class PFetiDPSolverTests2DSmall
	{
		private const double domainLengthX = 2.0, domainLengthY = 2.0;
		private const int singleSubdomainID = 0;
		private const int maxIterations = 1000;

		[Theory]
		// Homogeneous problem
		[InlineData(1.0, EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.Dense)]
		[InlineData(1.0, EnvironmentChoice.ManagedParSubSingleClus, MatrixFormat.Dense)]
		[InlineData(1.0, EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.CSparse)]
		[InlineData(1.0, EnvironmentChoice.ManagedParSubSingleClus, MatrixFormat.CSparse)]
		[InlineData(1.0, EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.CSparseSymmetric)]
		[InlineData(1.0, EnvironmentChoice.ManagedParSubSingleClus, MatrixFormat.CSparseSymmetric)]
		[InlineData(1.0, EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.SuiteSparseSymmetric)]
		public static void Run(double stiffnessRatio, EnvironmentChoice env, MatrixFormat matrixFormat)
		{
			IDdmEnvironment environment = env.Create();
			double pcgConvergenceTol = 1E-5;
			IVectorView directDisplacements = SolveModelWithoutSubdomains(stiffnessRatio);
			(IVectorView ddDisplacements, SolverLogger logger) = 
				SolveModelWithSubdomains(stiffnessRatio, environment, matrixFormat);
			double normalizedError = directDisplacements.Subtract(ddDisplacements).Norm2() / directDisplacements.Norm2();
			Assert.Equal(0.0, normalizedError, 6);
		}

		private static Model CreateModel(double stiffnessRatio)
		{

			double E0 = 2.1E7;
			double E1 = stiffnessRatio * E0;

			var builder = new Uniform2DModelBuilderYMajor();
			builder.DomainLengthX = domainLengthX;
			builder.DomainLengthY = domainLengthY;
			builder.NumSubdomainsX = 2;
			builder.NumSubdomainsY = 2;
			builder.NumTotalElementsX = 4;
			builder.NumTotalElementsY = 4;
			builder.YoungModulus = E0;
			builder.PrescribeDisplacement(Uniform2DModelBuilderYMajor.BoundaryRegion.LeftSide, StructuralDof.TranslationX, 0.0);
			builder.PrescribeDisplacement(Uniform2DModelBuilderYMajor.BoundaryRegion.LeftSide, StructuralDof.TranslationY, 0.0);
			builder.DistributeLoadAtNodes(Uniform2DModelBuilderYMajor.BoundaryRegion.UpperRightCorner, StructuralDof.TranslationY, 100.0);

			return builder.BuildModel();
		}

		private static Model CreateSingleSubdomainModel(double stiffnessRatio)
		{
			// Replace the existing subdomains with a single one
			Model model = CreateModel(stiffnessRatio);
			model.SubdomainsDictionary.Clear();
			var subdomain = new Subdomain(singleSubdomainID);
			model.SubdomainsDictionary.Add(singleSubdomainID, subdomain);
			foreach (Element element in model.Elements) subdomain.Elements.Add(element);
			return model;
		}

		private static (IVectorView globalDisplacements, SolverLogger logger) SolveModelWithSubdomains(double stiffnessRatio,
			IDdmEnvironment environment, MatrixFormat matrixFormat)
		{
			// Model
			Model model = CreateModel(stiffnessRatio);
			model.ConnectDataStructures();

			var clusters = new MGroup.Solvers.DDM.Cluster[1];
			clusters[0] = new MGroup.Solvers.DDM.Cluster(0);
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
			solverBuilder.ComputingEnvironment = environment;

			// Specify the format of P-FETI-DP's matrices
			if (matrixFormat == MatrixFormat.Dense)
			{
				solverBuilder.MatrixManagerFactory = new PFetiDPMatrixManagerFactoryDense();
				solverBuilder.CoarseProblemFactory = new FetiDPCoarseProblemDense.Factory();
			}
			else if (matrixFormat == MatrixFormat.CSparse)
			{
				solverBuilder.MatrixManagerFactory = new PFetiDPMatrixManagerFactoryCSparseNonSymmetric();
				solverBuilder.CoarseProblemFactory = new FetiDPCoarseProblemCSparseNonSymmetric.Factory();
			}
			else if (matrixFormat == MatrixFormat.CSparseSymmetric)
			{
				solverBuilder.MatrixManagerFactory = new PFetiDPMatrixManagerFactoryCSparse();
				solverBuilder.CoarseProblemFactory = new FetiDPCoarseProblemCSparse.Factory();
			}
			else if (matrixFormat == MatrixFormat.SuiteSparseSymmetric)
			{
				solverBuilder.MatrixManagerFactory = new PFetiDPMatrixManagerFactorySuiteSparse();
				solverBuilder.CoarseProblemFactory = new FetiDPCoarseProblemSuiteSparse.Factory();
			}
			else
			{
				throw new NotImplementedException();
			}


			// Specify solver for the interface problem of PSM
			//solverBuilder.InterfaceProblemSolver = new InterfaceProblemSolverPcg();
			//var gmres = new GmresAlgorithm.Builder().Build();
			//solverBuilder.InterfaceProblemSolver = new InterfaceProblemSolverGmres(gmres);

			PFetiDPSolver solver = solverBuilder.BuildSolver(model, cornerDofs, clusters);

			// Structural problem provider
			var provider = new ProblemStructural(model, solver);

			// Linear static analysis
			var childAnalyzer = new LinearAnalyzer(model, solver, provider);
			var parentAnalyzer = new StaticAnalyzer(model, solver, provider, childAnalyzer);

			// Run the analysis
			parentAnalyzer.Initialize();
			parentAnalyzer.Solve();

			// Gather the global displacements
			Vector globalDisplacements = solver.GatherGlobalDisplacements(model);

			return (globalDisplacements, solver.Logger);
		}

		private static IVectorView SolveModelWithoutSubdomains(double stiffnessRatio)
		{
			Model model = CreateSingleSubdomainModel(stiffnessRatio);

			// Solver
			SkylineSolver solver = (new SkylineSolver.Builder()).BuildSolver(model);
			//SuiteSparseSolver solver = new SuiteSparseSolver.Builder().BuildSolver(model);

			// Structural problem provider
			var provider = new ProblemStructural(model, solver);

			// Linear static analysis
			var childAnalyzer = new LinearAnalyzer(model, solver, provider);
			var parentAnalyzer = new StaticAnalyzer(model, solver, provider, childAnalyzer);

			// Run the analysis
			parentAnalyzer.Initialize();
			parentAnalyzer.Solve();

			return solver.LinearSystems[singleSubdomainID].Solution;
		}
	}
}
