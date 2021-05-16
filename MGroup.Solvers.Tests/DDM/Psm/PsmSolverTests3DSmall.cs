namespace MGroup.Tests.DDM.Psm
{
	using System;
	using System.Collections.Generic;
	using ISAAR.MSolve.LinearAlgebra.Vectors;
	using MGroup.Solvers.DDM.Psm;
	using MGroup.Solvers.DDM.Psm.StiffnessMatrices;
	using Xunit;
	using MGroup.Solvers.DDM.Environments;
    using ISAAR.MSolve.Solvers;
    using ISAAR.MSolve.FEM.Entities;
    using ISAAR.MSolve.Discretization.FreedomDegrees;
    using ISAAR.MSolve.Discretization.Interfaces;
    using ISAAR.MSolve.Problems;
    using ISAAR.MSolve.Analyzers;
    using ISAAR.MSolve.Solvers.Direct;

    public static class PsmPSolverTests3DSmall
	{
		private const double domainLengthX = 2.0, domainLengthY = 2.0, domainLengthZ = 2.0;
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
			int numIterations = logger.GetNumIterationsOfIterativeAlgorithm(0);
			Assert.Equal(0.0, normalizedError, 6);
			Assert.InRange(numIterations, 1, 26);
		}

		private static Model CreateModel(double stiffnessRatio)
		{
			double E0 = 2.1E7;
			double E1 = stiffnessRatio * E0;

			var builder = new Uniform3DModelBuilder();
			builder.MinX = 0;
			builder.MinY = 0;
			builder.MinZ = 0;
			builder.MaxX = domainLengthX;
			builder.MaxY = domainLengthY;
			builder.MaxZ = domainLengthZ;
			builder.NumSubdomainsX = 2;
			builder.NumSubdomainsY = 2;
			builder.NumSubdomainsZ = 2;
			builder.NumTotalElementsX = 4;
			builder.NumTotalElementsY = 4;
			builder.NumTotalElementsZ = 4;
			builder.YoungModulus = E0;
			builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MinX, StructuralDof.TranslationX, 0.0);
			builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MinX, StructuralDof.TranslationY, 0.0);
			builder.PrescribeDisplacement(Uniform3DModelBuilder.BoundaryRegion.MinX, StructuralDof.TranslationZ, 0.0);
			builder.DistributeLoadAtNodes(Uniform3DModelBuilder.BoundaryRegion.MaxX, StructuralDof.TranslationY, 100.0);

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

			// Solver
			var solverBuilder = new PsmSolver.Builder();
			solverBuilder.ComputingEnvironment = environment;

			// Specify the format of PSM's matrices
			if (matrixFormat == MatrixFormat.Dense)
			{
				solverBuilder.MatrixManagerFactory = new PsmMatrixManagerDense.Factory();
			}
			else if (matrixFormat == MatrixFormat.CSparse)
			{
				solverBuilder.MatrixManagerFactory = new PsmMatrixManagerCSparse.Factory();
			}
			else if (matrixFormat == MatrixFormat.CSparseSymmetric)
			{
				solverBuilder.MatrixManagerFactory = new PsmMatrixManagerSymmetricCSparse.Factory();
			}
			else if (matrixFormat == MatrixFormat.SuiteSparseSymmetric)
			{
				solverBuilder.MatrixManagerFactory = new PsmMatrixManagerSymmetricSuiteSparse.Factory();
			}
			else
			{
				throw new NotImplementedException();
			}

			// Specify solver for the interface problem of PSM
			//solverBuilder.InterfaceProblemSolver = new InterfaceProblemSolverPcg();
			//var gmres = new GmresAlgorithm.Builder().Build();
			//solverBuilder.InterfaceProblemSolver = new InterfaceProblemSolverGmres(gmres);

			PsmSolver solver = solverBuilder.BuildSolver(model, clusters);

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
			//var solver = new DenseMatrixSolver.Builder().BuildSolver(model);

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
