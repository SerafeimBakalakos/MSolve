using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Analyzers;
using ISAAR.MSolve.Analyzers.Multiscale;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Problems;
using ISAAR.MSolve.Solvers;
using ISAAR.MSolve.Solvers.Direct;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Tests.Utilities
{
    public static class Analysis
    {
        public static IMatrix RunHomogenizationAnalysisStructural2D(IXModel model,
            double[] minCoords, double[] maxCoords, double thickness, ISolverBuilder solverBuilder = null)
        {
            Console.WriteLine("Starting homogenization analysis");

            if (solverBuilder == null) solverBuilder = new SuiteSparseSolver.Builder();
            ISolver solver = solverBuilder.BuildSolver(model);
            var provider = new ProblemStructural(model, solver);
            var rve = new StructuralSquareRve(model, minCoords, maxCoords, thickness);
            var homogenization = new HomogenizationAnalyzer(model, solver, provider, rve);

            homogenization.Initialize();
            homogenization.Solve();
            IMatrix conductivity = homogenization.MacroscopicModulus;

            Console.WriteLine("Analysis finished");
            return conductivity;
        }

        public static IMatrix RunHomogenizationAnalysisStructural3D(IXModel model, double[] minCoords, double[] maxCoords, 
            ISolverBuilder solverBuilder = null)
        {
            Console.WriteLine("Starting homogenization analysis");
            if (solverBuilder == null) solverBuilder = new SuiteSparseSolver.Builder();
            ISolver solver = solverBuilder.BuildSolver(model);
            var provider = new ProblemStructural(model, solver);
            var rve = new StructuralCubicRve(model, minCoords, maxCoords);
            var homogenization = new HomogenizationAnalyzer(model, solver, provider, rve);

            homogenization.Initialize();
            homogenization.Solve();
            IMatrix conductivity = homogenization.MacroscopicModulus;

            Console.WriteLine("Analysis finished");
            return conductivity;
        }

        public static IMatrix RunHomogenizationAnalysisThermal2D(IXModel model, 
            double[] minCoords, double[] maxCoords, double thickness, bool calcMacroscopicFlux = false)
        {
            Console.WriteLine("Starting homogenization analysis");

            var solver = (new SuiteSparseSolver.Builder()).BuildSolver(model);
            var provider = new ProblemThermalSteadyState(model, solver);
            var rve = new ThermalSquareRve(model, minCoords, maxCoords, thickness);
            var homogenization = new HomogenizationAnalyzer(model, solver, provider, rve);
            if (calcMacroscopicFlux)
            {
                double[] temperatureGradient = { 200, 0 };
                homogenization.MacroscopicStrains = temperatureGradient;
            }

            homogenization.Initialize();
            homogenization.Solve();
            IMatrix conductivity = homogenization.MacroscopicModulus;

            Console.WriteLine("Analysis finished");
            return conductivity;
        }

        public static IMatrix RunHomogenizationAnalysisThermal3D(IXModel model, double[] minCoords, double[] maxCoords, 
            ISolverBuilder solverBuilder = null)
        {
            Console.WriteLine("Starting homogenization analysis");
            if (solverBuilder == null) solverBuilder = new SuiteSparseSolver.Builder();
            ISolver solver = solverBuilder.BuildSolver(model);
            var provider = new ProblemThermalSteadyState(model, solver);
            var rve = new ThermalCubicRve(model, minCoords, maxCoords);
            var homogenization = new HomogenizationAnalyzer(model, solver, provider, rve);

            homogenization.Initialize();
            homogenization.Solve();
            IMatrix conductivity = homogenization.MacroscopicModulus;

            Console.WriteLine("Analysis finished");
            return conductivity;
        }

        public static IVectorView RunThermalStaticAnalysis(IXModel model, ISolverBuilder solverBuilder = null)
        {
            Console.WriteLine("Starting analysis");
            if (solverBuilder == null) solverBuilder = new SkylineSolver.Builder();
            ISolver solver = solverBuilder.BuildSolver(model);
            var problem = new ProblemThermalSteadyState(model, solver);
            var linearAnalyzer = new LinearAnalyzer(model, solver, problem);
            var staticAnalyzer = new StaticAnalyzer(model, solver, problem, linearAnalyzer);

            staticAnalyzer.Initialize();
            staticAnalyzer.Solve();

            Console.WriteLine("Analysis finished");
            return solver.LinearSystems[0].Solution;
        }

        public static IVectorView RunStructuralStaticAnalysis(IXModel model, ISolverBuilder solverBuilder = null)
        {
            Console.WriteLine("Starting analysis");
            if (solverBuilder == null) solverBuilder = new SkylineSolver.Builder();
            ISolver solver = solverBuilder.BuildSolver(model);
            var problem = new ProblemStructural(model, solver);
            var linearAnalyzer = new LinearAnalyzer(model, solver, problem);
            var staticAnalyzer = new StaticAnalyzer(model, solver, problem, linearAnalyzer);

            staticAnalyzer.Initialize();
            staticAnalyzer.Solve();

            Console.WriteLine("Analysis finished");
            return solver.LinearSystems[0].Solution;
        }
    }
}
