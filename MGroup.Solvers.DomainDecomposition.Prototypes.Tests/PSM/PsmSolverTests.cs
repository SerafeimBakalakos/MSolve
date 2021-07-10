using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Analyzers;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Problems;
using MGroup.Solvers.DomainDecomposition.Prototypes.PSM;
using MGroup.Solvers.DomainDecomposition.Prototypes.Tests.Enums;
using MGroup.Solvers.DomainDecomposition.Prototypes.Tests.ExampleModels;
using Xunit;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.Tests.PSM
{
    public static class PsmSolverTests
    {
        [Theory]
        [InlineData(PsmInterfaceProblem.Global)]
        [InlineData(PsmInterfaceProblem.Distributed)]
        public static void TestForBrick3D(PsmInterfaceProblem psmInterfaceProblem)
        {
            // Model
            IStructuralModel model = Brick3DExample.CreateMultiSubdomainModel();
            model.ConnectDataStructures(); //TODOMPI: this is also done in the analyzer

            // Solver
            var solver = new PsmSolver(model, true, 1E-10, 200, psmInterfaceProblem.Create());

            // Linear static analysis
            var problem = new ProblemThermalSteadyState(model, solver);
            var childAnalyzer = new LinearAnalyzer(model, solver, problem);
            var parentAnalyzer = new StaticAnalyzer(model, solver, problem, childAnalyzer);

            // Run the analysis
            parentAnalyzer.Initialize();
            parentAnalyzer.Solve();

            // Check results
            Table<int, int, double> expectedResults = Brick3DExample.GetExpectedNodalValues();
            double tolerance = 1E-7;
            foreach (ISubdomain subdomain in model.Subdomains)
            {
                Table<int, int, double> computedResults =
                    Utilities.FindNodalFieldValues(subdomain, solver.LinearSystems[subdomain.ID].Solution);
                Utilities.AssertEqual(expectedResults, computedResults, tolerance);
            }

            //Debug.WriteLine($"Num PCG iterations = {solver.PcgStats.NumIterationsRequired}," +
            //    $" final residual norm ratio = {solver.PcgStats.ResidualNormRatioEstimation}");

            // Check convergence
            int precision = 10;
            int pcgIterationsExpected = 160;
            double pcgResidualNormRatioExpected = 7.487370033127084E-11;
            Assert.Equal(pcgIterationsExpected, solver.PcgStats.NumIterationsRequired);
            Assert.Equal(pcgResidualNormRatioExpected, solver.PcgStats.ResidualNormRatioEstimation, precision);
        }

        [Theory]
        [InlineData(PsmInterfaceProblem.Global)]
        [InlineData(PsmInterfaceProblem.Distributed)]
        public static void TestForLine1D(PsmInterfaceProblem psmInterfaceProblem)
        {
            // Model
            IStructuralModel model = Line1DExample.CreateMultiSubdomainModel();
            model.ConnectDataStructures(); //TODOMPI: this is also done in the analyzer

            // Solver
            var solver = new PsmSolver(model, true, 1E-10, 200, psmInterfaceProblem.Create());

            // Linear static analysis
            var problem = new ProblemThermalSteadyState(model, solver);
            var childAnalyzer = new LinearAnalyzer(model, solver, problem);
            var parentAnalyzer = new StaticAnalyzer(model, solver, problem, childAnalyzer);

            // Run the analysis
            parentAnalyzer.Initialize();
            parentAnalyzer.Solve();

            // Check results
            Table<int, int, double> expectedResults = Line1DExample.GetExpectedNodalValues();
            double tolerance = 1E-7;
            foreach (ISubdomain subdomain in model.Subdomains)
            {
                Table<int, int, double> computedResults =
                    Utilities.FindNodalFieldValues(subdomain, solver.LinearSystems[subdomain.ID].Solution);
                Utilities.AssertEqual(expectedResults, computedResults, tolerance);
            }

            //Debug.WriteLine($"Num PCG iterations = {solver.PcgStats.NumIterationsRequired}," +
            //    $" final residual norm ratio = {solver.PcgStats.ResidualNormRatioEstimation}");

            // Check convergence
            int precision = 10;
            int pcgIterationsExpected = 7;
            double pcgResidualNormRatioExpected = 0;
            Assert.Equal(pcgIterationsExpected, solver.PcgStats.NumIterationsRequired);
            Assert.Equal(pcgResidualNormRatioExpected, solver.PcgStats.ResidualNormRatioEstimation, precision);
        }

        [Theory]
        [InlineData(PsmInterfaceProblem.Global)]
        [InlineData(PsmInterfaceProblem.Distributed)]
        public static void TestForPlane2D(PsmInterfaceProblem psmInterfaceProblem)
        {
            // Model
            IStructuralModel model = Plane2DExample.CreateMultiSubdomainModel();
            model.ConnectDataStructures(); //TODOMPI: this is also done in the analyzer

            // Solver
            var solver = new PsmSolver(model, true, 1E-10, 200, psmInterfaceProblem.Create());

            // Linear static analysis
            var problem = new ProblemThermalSteadyState(model, solver);
            var childAnalyzer = new LinearAnalyzer(model, solver, problem);
            var parentAnalyzer = new StaticAnalyzer(model, solver, problem, childAnalyzer);

            // Run the analysis
            parentAnalyzer.Initialize();
            parentAnalyzer.Solve();

            // Check results
            Table<int, int, double> expectedResults = Plane2DExample.GetExpectedNodalValues();
            double tolerance = 1E-7;
            foreach (ISubdomain subdomain in model.Subdomains)
            {
                Table<int, int, double> computedResults =
                    Utilities.FindNodalFieldValues(subdomain, solver.LinearSystems[subdomain.ID].Solution);
                Utilities.AssertEqual(expectedResults, computedResults, tolerance);
            }

            //Debug.WriteLine($"Num PCG iterations = {solver.PcgStats.NumIterationsRequired}," +
            //    $" final residual norm ratio = {solver.PcgStats.ResidualNormRatioEstimation}");

            // Check convergence
            int precision = 10;
            int pcgIterationsExpected = 63;
            double pcgResidualNormRatioExpected = 4.859075883397028E-11;
            Assert.Equal(pcgIterationsExpected, solver.PcgStats.NumIterationsRequired);
            Assert.Equal(pcgResidualNormRatioExpected, solver.PcgStats.ResidualNormRatioEstimation, precision);
        }
    }
}
