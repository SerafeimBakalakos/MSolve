using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Iterative.Termination;
using ISAAR.MSolve.Solvers.Direct;
using MGroup.Analyzers;
using MGroup.Environments;
using MGroup.LinearAlgebra.Distributed.IterativeMethods;
using MGroup.Problems;
using MGroup.Solvers.DomainDecomposition.Psm;
using MGroup.Solvers.DomainDecomposition.PSM.Dofs;
using MGroup.Solvers.DomainDecomposition.Tests.ExampleModels;
using TriangleNet;
using Xunit;

namespace MGroup.Solvers.DomainDecomposition.Tests.PSM
{
    public static class PsmSolverTests
    {
        [Theory]
        [InlineData(EnvironmentChoice.SequentialSharedEnvironment)]
        [InlineData(EnvironmentChoice.TplSharedEnvironment)]
        public static void TestForBrick3D(EnvironmentChoice environmentChoice)
            => TestForBrick3DInternal(Utilities.CreateEnvironment(environmentChoice));

        internal static void TestForBrick3DInternal(IComputeEnvironment environment)
        {
            // Environment
            ComputeNodeTopology nodeTopology = Brick3DExample.CreateNodeTopology();
            environment.Initialize(nodeTopology);

            // Model
            IStructuralModel model = Brick3DExample.CreateMultiSubdomainModel(environment);
            model.ConnectDataStructures(); //TODOMPI: this is also done in the analyzer
            var subdomainTopology = new SubdomainTopology(environment, model);

            // Solver
            var pcgBuilder = new PcgAlgorithm.Builder();
            pcgBuilder.MaxIterationsProvider = new FixedMaxIterationsProvider(200);
            pcgBuilder.ResidualTolerance = 1E-10;
            var solverBuilder = new PsmSolver.Builder(environment);
            solverBuilder.InterfaceProblemSolver = pcgBuilder.Build();
            PsmSolver solver = solverBuilder.BuildSolver(model, subdomainTopology);

            // Linear static analysis
            var problem = new ProblemThermalSteadyState(environment, model, solver);
            var childAnalyzer = new LinearAnalyzer(environment, model, solver, problem);
            var parentAnalyzer = new StaticAnalyzer(environment, model, solver, problem, childAnalyzer);

            // Run the analysis
            parentAnalyzer.Initialize();
            parentAnalyzer.Solve();

            // Check results
            Table<int, int, double> expectedResults = Brick3DExample.GetExpectedNodalValues();
            double tolerance = 1E-7;
            environment.DoPerNode(subdomainID =>
            {
                ISubdomain subdomain = model.GetSubdomain(subdomainID);
                Table<int, int, double> computedResults =
                    Utilities.FindNodalFieldValues(subdomain, solver.LinearSystems[subdomainID].Solution);
                Utilities.AssertEqual(expectedResults, computedResults, tolerance);
            });
        }

        [Theory]
        [InlineData(EnvironmentChoice.SequentialSharedEnvironment)]
        [InlineData(EnvironmentChoice.TplSharedEnvironment)]
        public static void TestForLine1D(EnvironmentChoice environmentChoice)
            => TestForLine1DInternal(Utilities.CreateEnvironment(environmentChoice));

        internal static void TestForLine1DInternal(IComputeEnvironment environment)
        {
            // Environment
            ComputeNodeTopology nodeTopology = Line1DExample.CreateNodeTopology();
            environment.Initialize(nodeTopology);

            // Model
            IStructuralModel model = Line1DExample.CreateMultiSubdomainModel(environment);
            model.ConnectDataStructures(); //TODOMPI: this is also done in the analyzer
            var subdomainTopology = new SubdomainTopology(environment, model);

            // Solver
            var solverBuilder = new PsmSolver.Builder(environment);
            PsmSolver solver = solverBuilder.BuildSolver(model, subdomainTopology);

            // Linear static analysis
            var problem = new ProblemThermalSteadyState(environment, model, solver);
            var childAnalyzer = new LinearAnalyzer(environment, model, solver, problem);
            var parentAnalyzer = new StaticAnalyzer(environment, model, solver, problem, childAnalyzer);

            // Run the analysis
            parentAnalyzer.Initialize();
            parentAnalyzer.Solve();

            // Check results
            Table<int, int, double> expectedResults = Line1DExample.GetExpectedNodalValues();
            double tolerance = 1E-7;
            environment.DoPerNode(subdomainID =>
            {
                ISubdomain subdomain = model.GetSubdomain(subdomainID);
                Table<int, int, double> computedResults = 
                    Utilities.FindNodalFieldValues(subdomain, solver.LinearSystems[subdomainID].Solution);
                Utilities.AssertEqual(expectedResults, computedResults, tolerance);
            });
        }

        [Theory]
        [InlineData(EnvironmentChoice.SequentialSharedEnvironment)]
        [InlineData(EnvironmentChoice.TplSharedEnvironment)]
        public static void TestForPlane2D(EnvironmentChoice environmentChoice)
            => TestForPlane2DInternal(Utilities.CreateEnvironment(environmentChoice));

        internal static void TestForPlane2DInternal(IComputeEnvironment environment)
        {
            // Environment
            ComputeNodeTopology nodeTopology = Plane2DExample.CreateNodeTopology();
            environment.Initialize(nodeTopology);

            // Model
            IStructuralModel model = Plane2DExample.CreateMultiSubdomainModel(environment);
            model.ConnectDataStructures(); //TODOMPI: this is also done in the analyzer
            var subdomainTopology = new SubdomainTopology(environment, model);

            // Solver
            var pcgBuilder = new PcgAlgorithm.Builder();
            pcgBuilder.MaxIterationsProvider = new FixedMaxIterationsProvider(100);
            pcgBuilder.ResidualTolerance = 1E-10;
            var solverBuilder = new PsmSolver.Builder(environment);
            solverBuilder.InterfaceProblemSolver = pcgBuilder.Build();
            PsmSolver solver = solverBuilder.BuildSolver(model, subdomainTopology);

            // Linear static analysis
            var problem = new ProblemThermalSteadyState(environment, model, solver);
            var childAnalyzer = new LinearAnalyzer(environment, model, solver, problem);
            var parentAnalyzer = new StaticAnalyzer(environment, model, solver, problem, childAnalyzer);

            // Run the analysis
            parentAnalyzer.Initialize();
            parentAnalyzer.Solve();

            // Check results
            Table<int, int, double> expectedResults = Plane2DExample.GetExpectedNodalValues();
            double tolerance = 1E-7;
            environment.DoPerNode(subdomainID =>
            {
                ISubdomain subdomain = model.GetSubdomain(subdomainID);
                Table<int, int, double> computedResults =
                    Utilities.FindNodalFieldValues(subdomain, solver.LinearSystems[subdomainID].Solution);
                Utilities.AssertEqual(expectedResults, computedResults, tolerance);
            });
        }
    }
}
