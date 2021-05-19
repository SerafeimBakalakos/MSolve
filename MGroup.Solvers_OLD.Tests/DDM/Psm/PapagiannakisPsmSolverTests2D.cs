namespace MGroup.Tests.DDM.Psm
{
	using System;
    using ISAAR.MSolve.Analyzers;
    using ISAAR.MSolve.Discretization.FreedomDegrees;
    using ISAAR.MSolve.Discretization.Interfaces;
    using ISAAR.MSolve.FEM.Entities;
    using ISAAR.MSolve.LinearAlgebra.Iterative.GeneralizedMinimalResidual;
	using ISAAR.MSolve.LinearAlgebra.Iterative.PreconditionedConjugateGradient;
	using ISAAR.MSolve.LinearAlgebra.Iterative.Termination;
	using ISAAR.MSolve.LinearAlgebra.Vectors;
    using ISAAR.MSolve.Problems;
    using ISAAR.MSolve.Solvers;
    using ISAAR.MSolve.Solvers.Direct;
    using MGroup.Solvers_OLD.DDM.Environments;
	using MGroup.Solvers_OLD.DDM.Psm;
	using MGroup.Solvers_OLD.DDM.Psm.InterfaceProblem;
	using MGroup.Solvers_OLD.DDM.Psm.Preconditioner;
	using MGroup.Solvers_OLD.DDM.Psm.StiffnessMatrices;
	using Xunit;

	/// <summary>
	/// Tests from Papagiannakis bachelor thesis (NTUA 2011), p. 134 - 147
	/// Authors: Serafeim Bakalakos
	/// </summary>
	public static class PapagiannakisPsmSolverTests2D
	{
		private const double domainLengthX = 3.0, domainLengthY = 1.5;
		private const int singleSubdomainID = 0;
		private const int maxIterations = 1000;

		public enum Preconditioner { Identity, Direct }
		public enum IterativeSolver { PCG, GMRES }

		[Theory]
		// Homogeneous problem
		[InlineData(1.0, EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.Dense, Preconditioner.Identity, IterativeSolver.PCG, 50)]
		[InlineData(1.0, EnvironmentChoice.ManagedParSubSingleClus, MatrixFormat.Dense, Preconditioner.Identity, IterativeSolver.PCG, 50)]
		[InlineData(1.0, EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.Dense, Preconditioner.Identity, IterativeSolver.GMRES, 63)]
		[InlineData(1.0, EnvironmentChoice.ManagedParSubSingleClus, MatrixFormat.Dense, Preconditioner.Identity, IterativeSolver.GMRES, 63)]
		[InlineData(1.0, EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.CSparseSymmetric, Preconditioner.Identity, IterativeSolver.PCG, 50)]
		[InlineData(1.0, EnvironmentChoice.ManagedParSubSingleClus, MatrixFormat.CSparseSymmetric, Preconditioner.Identity, IterativeSolver.PCG, 50)]
		[InlineData(1.0, EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.CSparse, Preconditioner.Identity, IterativeSolver.PCG, 50)]
		[InlineData(1.0, EnvironmentChoice.ManagedParSubSingleClus, MatrixFormat.CSparse, Preconditioner.Identity, IterativeSolver.PCG, 50)]
		[InlineData(1.0, EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.CSparse, Preconditioner.Identity, IterativeSolver.GMRES, 63)]
		[InlineData(1.0, EnvironmentChoice.ManagedParSubSingleClus, MatrixFormat.CSparse, Preconditioner.Identity, IterativeSolver.GMRES, 63)]
		[InlineData(1.0, EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.SuiteSparseSymmetric, Preconditioner.Identity, IterativeSolver.PCG, 50)]

		[InlineData(1.0, EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.Dense, Preconditioner.Direct, IterativeSolver.PCG, 1)]
		[InlineData(1.0, EnvironmentChoice.ManagedParSubSingleClus, MatrixFormat.Dense, Preconditioner.Direct, IterativeSolver.PCG, 1)]
		//[InlineData(1.0, MatrixFormat.Dense, Preconditioner.Direct, IterativeSolver.GMRES, 1)]
		[InlineData(1.0, EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.CSparseSymmetric, Preconditioner.Direct, IterativeSolver.PCG, 1)]
		[InlineData(1.0, EnvironmentChoice.ManagedParSubSingleClus, MatrixFormat.CSparseSymmetric, Preconditioner.Direct, IterativeSolver.PCG, 1)]
		[InlineData(1.0, EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.CSparse, Preconditioner.Direct, IterativeSolver.PCG, 1)]
		[InlineData(1.0, EnvironmentChoice.ManagedParSubSingleClus, MatrixFormat.CSparse, Preconditioner.Direct, IterativeSolver.PCG, 1)]
		//[InlineData(1.0, MatrixFormat.CSparse, Preconditioner.Direct, IterativeSolver.GMRES, 1)]
		[InlineData(1.0, EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.SuiteSparseSymmetric, Preconditioner.Direct, IterativeSolver.PCG, 1)]

		// WARNING: PSM takes too many iterations or does not converge without a preconditioner in heterogeneous problems
		//[InlineData(1E2, MatrixFormat.Dense, Preconditioner.Identity, IterativeSolver.PCG, 50)]
		[InlineData(1E2, EnvironmentChoice.ManagedSeqSubSingleClus, MatrixFormat.Dense, Preconditioner.Direct, IterativeSolver.PCG, 1)]
		[InlineData(1E2, EnvironmentChoice.ManagedParSubSingleClus, MatrixFormat.Dense, Preconditioner.Direct, IterativeSolver.PCG, 1)]
		//[InlineData(1E2, MatrixFormat.Dense, Preconditioner.Identity, IterativeSolver.GMRES, 63)]
		//[InlineData(1E2, MatrixFormat.CSparseSymmetric, Preconditioner.Identity, IterativeSolver.PCG, 50)]
		//[InlineData(1E2, MatrixFormat.CSparse, Preconditioner.Identity, IterativeSolver.PCG, 50)]
		//[InlineData(1E2, MatrixFormat.CSparse, Preconditioner.Identity, IterativeSolver.GMRES, 63)]
		//[InlineData(1E2, MatrixFormat.SuiteSparseSymmetric, Preconditioner.Identity, IterativeSolver.PCG, 50)]

		//[InlineData(1E3, MatrixFormat.Dense, Preconditioner.Identity, IterativeSolver.PCG, 50)]
		//[InlineData(1E3, MatrixFormat.Dense, Preconditioner.Identity, IterativeSolver.GMRES, 63)]
		//[InlineData(1E3, MatrixFormat.CSparseSymmetric, Preconditioner.Identity, IterativeSolver.PCG, 50)]
		//[InlineData(1E3, MatrixFormat.CSparse, Preconditioner.Identity, IterativeSolver.PCG, 50)]
		//[InlineData(1E3, MatrixFormat.CSparse, Preconditioner.Identity, IterativeSolver.GMRES, 63)]
		//[InlineData(1E3, MatrixFormat.SuiteSparseSymmetric, Preconditioner.Identity, IterativeSolver.PCG, 50)]

		//[InlineData(1E4, MatrixFormat.Dense, Preconditioner.Identity, IterativeSolver.PCG, 50)]
		//[InlineData(1E4, MatrixFormat.Dense, Preconditioner.Identity, IterativeSolver.GMRES, 63)]
		//[InlineData(1E4, MatrixFormat.CSparseSymmetric, Preconditioner.Identity, IterativeSolver.PCG, 50)]
		//[InlineData(1E4, MatrixFormat.CSparse, Preconditioner.Identity, IterativeSolver.PCG, 50)]
		//[InlineData(1E4, MatrixFormat.CSparse, Preconditioner.Identity, IterativeSolver.GMRES, 63)]
		//[InlineData(1E4, MatrixFormat.SuiteSparseSymmetric, Preconditioner.Identity, IterativeSolver.PCG, 50)]

		//[InlineData(1E5, MatrixFormat.Dense, Preconditioner.Identity, IterativeSolver.PCG, 50)]
		//[InlineData(1E5, MatrixFormat.Dense, Preconditioner.Identity, IterativeSolver.GMRES, 63)]
		//[InlineData(1E5, MatrixFormat.CSparseSymmetric, Preconditioner.Identity, IterativeSolver.PCG, 50)]
		//[InlineData(1E5, MatrixFormat.CSparse, Preconditioner.Identity, IterativeSolver.PCG, 50)]
		//[InlineData(1E5, MatrixFormat.CSparse, Preconditioner.Identity, IterativeSolver.GMRES, 63)]
		//[InlineData(1E5, MatrixFormat.SuiteSparseSymmetric, Preconditioner.Identity, IterativeSolver.PCG, 50)]

		//[InlineData(1E6, MatrixFormat.Dense, Preconditioner.Identity, IterativeSolver.PCG, 50)]
		//[InlineData(1E6, MatrixFormat.Dense, Preconditioner.Identity, IterativeSolver.GMRES, 63)]
		//[InlineData(1E6, MatrixFormat.CSparseSymmetric, Preconditioner.Identity, IterativeSolver.PCG, 50)]
		//[InlineData(1E6, MatrixFormat.CSparse, Preconditioner.Identity, IterativeSolver.PCG, 50)]
		//[InlineData(1E6, MatrixFormat.CSparse, Preconditioner.Identity, IterativeSolver.GMRES, 63)]
		//[InlineData(1E6, MatrixFormat.SuiteSparseSymmetric, Preconditioner.Identity, IterativeSolver.PCG, 50)]
		public static void Run(double stiffnessRatio, EnvironmentChoice env, MatrixFormat matrixFormat, Preconditioner precond, 
			IterativeSolver iterativeSolver, int iterExpected)
		{
			IDdmEnvironment environment = env.Create();

			double iterativeSolverTolerance = 1E-5;
			IVectorView directDisplacements = SolveModelWithoutSubdomains(stiffnessRatio);
			(IVectorView ddDisplacements, SolverLogger logger) = SolveModelWithSubdomains(
				stiffnessRatio, environment, matrixFormat, precond, iterativeSolver, iterativeSolverTolerance);

			int analysisStep = 0;
			Assert.Equal(160, logger.GetNumDofs(analysisStep, "Global boundary dofs"));

			double normalizedError = directDisplacements.Subtract(ddDisplacements).Norm2() / directDisplacements.Norm2();
			Assert.Equal(0.0, normalizedError, 6);

			// Allow some tolerance for the iterations:
			int maxIterationsForApproximateResidual = iterExpected + 1;
			int pcgIterations = logger.GetNumIterationsOfIterativeAlgorithm(analysisStep);
			Assert.InRange(pcgIterations, 1, maxIterationsForApproximateResidual); // the upper bound is inclusive!
		}

		private static Model CreateModel(double stiffnessRatio)
		{
			// Subdomains:
			// /|
			// /||-------|-------|-------|-------|
			// /||  (4)  |  (5)  |  (6)  |  (7)  |
			// /||   E1  |   E0  |   E0  |   E0  |
			// /||-------|-------|-------|-------|
			// /||  (0)  |  (1)  |  (2)  |  (3)  |
			// /||   E1  |   E0  |   E0  |   E0  |
			// /||-------|-------|-------|-------|
			// /|

			double E0 = 2.1E7;
			double E1 = stiffnessRatio * E0;

			var builder = new Uniform2DModelBuilder();
			builder.DomainLengthX = domainLengthX;
			builder.DomainLengthY = domainLengthY;
			builder.NumSubdomainsX = 4;
			builder.NumSubdomainsY = 2;
			builder.NumTotalElementsX = 20;
			builder.NumTotalElementsY = 20;
			builder.YoungModuliOfSubdomains = new double[,] { { E1, E0, E0, E0 }, { E1, E0, E0, E0 } };
			builder.PrescribeDisplacement(Uniform2DModelBuilder.BoundaryRegion.LeftSide, StructuralDof.TranslationX, 0.0);
			builder.PrescribeDisplacement(Uniform2DModelBuilder.BoundaryRegion.LeftSide, StructuralDof.TranslationY, 0.0);
			builder.DistributeLoadAtNodes(Uniform2DModelBuilder.BoundaryRegion.RightSide, StructuralDof.TranslationY, 100.0);

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
			IDdmEnvironment environment, MatrixFormat matrixFormat, Preconditioner precond, 
			IterativeSolver iterativeSolver, double iterativeSolverTolerance)
		{
			// Model
			Model multiSubdomainModel = CreateModel(stiffnessRatio);
			var clusters = new MGroup.Solvers_OLD.DDM.Cluster[1];
			clusters[0] = new MGroup.Solvers_OLD.DDM.Cluster(0);
			foreach (ISubdomain subdomain in multiSubdomainModel.Subdomains)
			{
				clusters[0].Subdomains.Add(subdomain);
			}

			// Solver
			var solverBuilder = new PsmSolver.Builder();
			solverBuilder.ComputingEnvironment = environment;

			// Homogeneous/heterogeneous stiffness distribution
			solverBuilder.IsHomogeneousProblem = false;
			//solverBuilder.IsHomogeneousProblem = stiffnessRatio == 1.0;

			// Specify the format of PSM's matrices
			if (matrixFormat == MatrixFormat.Dense)
			{
				solverBuilder.MatrixManagerFactory = new PsmMatrixManagerDense.Factory();
			}
			else if (matrixFormat == MatrixFormat.CSparseSymmetric)
			{
				solverBuilder.MatrixManagerFactory = new PsmMatrixManagerSymmetricCSparse.Factory();
			}
			else if (matrixFormat == MatrixFormat.CSparse)
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

			// Specify preconditioner for PSM
			if (precond == Preconditioner.Identity)
			{
				solverBuilder.Preconditioner = new PsmPreconditionerIdentity();
			}
			else if (precond == Preconditioner.Direct)
			{
				solverBuilder.Preconditioner = new PsmPreconditionerDirect();
			}
			else
			{
				throw new NotImplementedException();
			}

			// Specify solver for the interface problem of PSM
			if (iterativeSolver == IterativeSolver.PCG)
			{
				var pcgBuilder = new PcgAlgorithm.Builder();
				pcgBuilder.ResidualTolerance = iterativeSolverTolerance;
				pcgBuilder.MaxIterationsProvider = new PercentageMaxIterationsProvider(1.0);
				solverBuilder.InterfaceProblemSolver = new InterfaceProblemSolverPcg(pcgBuilder.Build());
			}
			else if (iterativeSolver == IterativeSolver.GMRES)
			{
				var gmresBuilder = new GmresAlgorithm.Builder();
				gmresBuilder.RelativeTolerance = 1E-2 * iterativeSolverTolerance;
				gmresBuilder.AbsoluteTolerance = 1E-2 * iterativeSolverTolerance;
				solverBuilder.InterfaceProblemSolver = new InterfaceProblemSolverGmres(gmresBuilder.Build());
			}

			PsmSolver solver = solverBuilder.BuildSolver(multiSubdomainModel, clusters);

			// Structural problem provider
			var provider = new ProblemStructural(multiSubdomainModel, solver);

			// Linear static analysis
			var childAnalyzer = new LinearAnalyzer(multiSubdomainModel, solver, provider);
			var parentAnalyzer = new StaticAnalyzer(multiSubdomainModel, solver, provider, childAnalyzer);

			// Run the analysis
			parentAnalyzer.Initialize();
			parentAnalyzer.Solve();

			// Gather the global displacements
			Vector globalDisplacements = solver.GatherGlobalDisplacements(multiSubdomainModel);

			return (globalDisplacements, solver.Logger);
		}

		private static IVectorView SolveModelWithoutSubdomains(double stiffnessRatio)
		{
			Model model = CreateSingleSubdomainModel(stiffnessRatio);

			// Solver
			SkylineSolver solver = (new SkylineSolver.Builder()).BuildSolver(model);
			//SuiteSparseSymmetricSolver solver = new SuiteSparseSymmetricSolver.Builder().BuildSolver(model);

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
