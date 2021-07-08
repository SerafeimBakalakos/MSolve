﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Analyzers;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Iterative.Termination;
using ISAAR.MSolve.Problems;
using ISAAR.MSolve.Solvers.Direct;
using MGroup.Environments;
using MGroup.LinearAlgebra.Distributed.IterativeMethods;
using MGroup.Solvers.DomainDecomposition.Prototypes.PSM;
using MGroup.Solvers.DomainDecomposition.Prototypes.Tests.ExampleModels;
using Xunit;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.Tests.PSM
{
    public static class PsmSolverTests
    {
        [Fact]
        public static void TestForBrick3D()
        {
            // Model
            IStructuralModel model = Brick3DExample.CreateMultiSubdomainModel();
            model.ConnectDataStructures(); //TODOMPI: this is also done in the analyzer

            // Solver
            var solver = new PsmSolver(model, true, 1E-10, 200);

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

            Debug.WriteLine($"Num PCG iterations = {solver.PcgStats.NumIterationsRequired}," +
                $" final residual norm ratio = {solver.PcgStats.ResidualNormRatioEstimation}");
        }

        [Fact]
        public static void TestForLine1DInternal()
        {
            // Model
            IStructuralModel model = Line1DExample.CreateMultiSubdomainModel();
            model.ConnectDataStructures(); //TODOMPI: this is also done in the analyzer

            // Solver
            var solver = new PsmSolver(model, true, 1E-10, 200);

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

            Debug.WriteLine($"Num PCG iterations = {solver.PcgStats.NumIterationsRequired}," +
                $" final residual norm ratio = {solver.PcgStats.ResidualNormRatioEstimation}");
        }

        [Fact]
        public static void TestForPlane2DInternal()
        {
            // Model
            IStructuralModel model = Plane2DExample.CreateMultiSubdomainModel();
            model.ConnectDataStructures(); //TODOMPI: this is also done in the analyzer

            // Solver
            var solver = new PsmSolver(model, true, 1E-10, 200);

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

            Debug.WriteLine($"Num PCG iterations = {solver.PcgStats.NumIterationsRequired}," +
                $" final residual norm ratio = {solver.PcgStats.ResidualNormRatioEstimation}");
        }
    }
}
