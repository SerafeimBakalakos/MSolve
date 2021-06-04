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
    public static class Line1DPsmSolverTest
    {
        public static void RunMpiTests()
        {
            // Launch 4 processes
            using (var mpiEnvironment = new MpiEnvironment())
            {
                MpiDebugUtilities.AssistDebuggerAttachment();

                TestSolver(mpiEnvironment);

                MpiDebugUtilities.DoSerially(MPI.Communicator.world,
                    () => Console.WriteLine($"Process {MPI.Communicator.world.Rank}: All tests passed"));
            }
        }

        [Theory]
        [InlineData(EnvironmentChoice.SequentialSharedEnvironment)]
        [InlineData(EnvironmentChoice.TplSharedEnvironment)]
        public static void TestSolverManaged(EnvironmentChoice environmentChoice)
            => TestSolver(Utilities.CreateEnvironment(environmentChoice));

        internal static void TestSolver(IComputeEnvironment environment)
        {
            // Environment
            ComputeNodeTopology nodeTopology = Line1DExample.CreateNodeTopology(environment);
            environment.Initialize(nodeTopology);

            // Model
            Model model = Line1DExample.CreateMultiSubdomainModel();
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
            Table<int, int, double> expectedResults = GetExpectedValues();
            double tolerance = 1E-7;
            environment.DoPerNode(subdomainID =>
            {
                ISubdomain subdomain = model.GetSubdomain(subdomainID);
                Table<int, int, double> computedResults = 
                    Utilities.FindNodalFieldValues(subdomain, solver.LinearSystems[subdomainID].Solution);
                Utilities.AssertEqual(expectedResults, computedResults, tolerance);
            });
        }

        private static Table<int, int, double> GetExpectedValues()
        {
            //var model = Line1DExample.CreateSingleSubdomainModel();
            //var solver = new ISAAR.MSolve.Solvers.Direct.SkylineSolver.Builder().BuildSolver(model);
            //var problem = new ISAAR.MSolve.Problems.ProblemThermalSteadyState(model, solver);
            //var childAnalyzer = new ISAAR.MSolve.Analyzers.LinearAnalyzer(model, solver, problem);
            //var parentAnalyzer = new ISAAR.MSolve.Analyzers.StaticAnalyzer(model, solver, problem, childAnalyzer);
            //parentAnalyzer.Initialize();
            //parentAnalyzer.Solve();
            //Table<int, int, double> result =
            //    Utilities.FindNodalFieldValues(model.Subdomains.First(), solver.LinearSystems.First().Value.Solution);

            var result = new Table<int, int, double>();
            result[0, 0] = 32;
            result[1, 0] = 30;
            result[2, 0] = 28;
            result[3, 0] = 26;
            result[4, 0] = 24;
            result[5, 0] = 22;
            result[6, 0] = 20;
            result[7, 0] = 18;
            result[8, 0] = 16;
            result[9, 0] = 14;
            result[10, 0] = 12;
            result[11, 0] = 10;
            result[12, 0] = 8;
            result[13, 0] = 6;
            result[14, 0] = 4;
            result[15, 0] = 2;
            result[16, 0] = 0;

            return result;
        }

    }
}
