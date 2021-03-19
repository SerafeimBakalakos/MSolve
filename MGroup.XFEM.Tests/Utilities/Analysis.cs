using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Analyzers;
using ISAAR.MSolve.Analyzers.Multiscale;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Problems;
using ISAAR.MSolve.Solvers.Direct;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Tests.Utilities
{
    public static class Analysis
    {
        public static IMatrix RunHomogenizationAnalysisStructural2D(IXModel model,
            double[] minCoords, double[] maxCoords, double thickness)
        {
            Console.WriteLine("Starting homogenization analysis");

            var solver = (new SuiteSparseSolver.Builder()).BuildSolver(model);
            var provider = new ProblemStructural(model, solver);
            var rve = new StructuralSquareRve(model, Vector2.Create(minCoords[0], minCoords[1]),
                Vector2.Create(maxCoords[0], maxCoords[1]), thickness);
            var homogenization = new HomogenizationAnalyzer(model, solver, provider, rve);

            homogenization.Initialize();
            homogenization.Solve();
            IMatrix conductivity = homogenization.EffectiveConstitutiveTensors[model.Subdomains[0].ID];

            Console.WriteLine("Analysis finished");
            return conductivity;
        }

        public static IMatrix RunHomogenizationAnalysisThermal2D(IXModel model, 
            double[] minCoords, double[] maxCoords, double thickness)
        {
            Console.WriteLine("Starting homogenization analysis");

            Vector2 temperatureGradient = Vector2.Create(200, 0);
            var solver = (new SuiteSparseSolver.Builder()).BuildSolver(model);
            var provider = new ProblemThermalSteadyState(model, solver);
            var rve = new ThermalSquareRve(model, Vector2.Create(minCoords[0], minCoords[1]),
                Vector2.Create(maxCoords[0], maxCoords[1]), thickness, temperatureGradient);
            var homogenization = new HomogenizationAnalyzer(model, solver, provider, rve);

            homogenization.Initialize();
            homogenization.Solve();
            IMatrix conductivity = homogenization.EffectiveConstitutiveTensors[model.Subdomains[0].ID];

            Console.WriteLine("Analysis finished");
            return conductivity;
        }

        public static IMatrix RunHomogenizationAnalysisThermal3D(IXModel model, double[] minCoords, double[] maxCoords)
        {
            Console.WriteLine("Starting homogenization analysis");
            var solver = (new SuiteSparseSolver.Builder()).BuildSolver(model);
            var provider = new ProblemThermalSteadyState(model, solver);
            var rve = new ThermalCubicRve(model, minCoords, maxCoords);
            var homogenization = new HomogenizationAnalyzer(model, solver, provider, rve);

            homogenization.Initialize();
            homogenization.Solve();
            IMatrix conductivity = homogenization.EffectiveConstitutiveTensors[model.Subdomains[0].ID];

            Console.WriteLine("Analysis finished");
            return conductivity;
        }

        public static IVectorView RunThermalStaticAnalysis(IXModel model)
        {
            Console.WriteLine("Starting analysis");
            //SuiteSparseSolver solver = new SuiteSparseSolver.Builder().BuildSolver(model);
            SkylineSolver solver = new SkylineSolver.Builder().BuildSolver(model);
            var problem = new ProblemThermalSteadyState(model, solver);
            var linearAnalyzer = new LinearAnalyzer(model, solver, problem);
            var staticAnalyzer = new StaticAnalyzer(model, solver, problem, linearAnalyzer);

            staticAnalyzer.Initialize();
            staticAnalyzer.Solve();

            Console.WriteLine("Analysis finished");
            return solver.LinearSystems[0].Solution;
        }

        public static IVectorView RunStructuralStaticAnalysis(IXModel model)
        {
            Console.WriteLine("Starting analysis");
            //SuiteSparseSolver solver = new SuiteSparseSolver.Builder().BuildSolver(model);
            SkylineSolver solver = new SkylineSolver.Builder().BuildSolver(model);
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
