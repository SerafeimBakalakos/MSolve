using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using MGroup.Analyzers;
using MGroup.Environments;
using MGroup.Environments.Mpi;
using MGroup.LinearAlgebra.Distributed.Overlapping;
using MGroup.Problems;
using MGroup.Solvers.DomainDecomposition.Psm;
using MGroup.Solvers.DomainDecomposition.PSM.Dofs;
using MGroup.Solvers.DomainDecomposition.Tests.ExampleModels;
using Xunit;

namespace MGroup.Solvers.DomainDecomposition.Tests.PSM
{
    public static class PsmSolverTests
    {
        [Theory]
        [InlineData(EnvironmentChoice.SequentialSharedEnvironment)]
        [InlineData(EnvironmentChoice.TplSharedEnvironment)]
        public static void TestInLine1DExampleManaged(EnvironmentChoice environmentChoice)
            => TestInLine1DExample(Utilities.CreateEnvironment(environmentChoice));

        internal static void TestInLine1DExample(IComputeEnvironment environment)
        {
            // Environment
            ComputeNodeTopology nodeTopology = Line1DExample.CreateNodeTopology(environment);
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
    }
}
