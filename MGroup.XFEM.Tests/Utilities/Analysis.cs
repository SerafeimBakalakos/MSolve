using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Analyzers;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Problems;
using ISAAR.MSolve.Solvers.Direct;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Tests.Utilities
{
    public static class Analysis
    {
        public static IVectorView RunStaticAnalysis(XModel model)
        {
            Console.WriteLine("Starting analysis");
            SuiteSparseSolver solver = new SuiteSparseSolver.Builder().BuildSolver(model);
            //SkylineSolver solver = new SkylineSolver.Builder().BuildSolver(model);
            var problem = new ProblemThermalSteadyState(model, solver);
            var linearAnalyzer = new LinearAnalyzer(model, solver, problem);
            var staticAnalyzer = new StaticAnalyzer(model, solver, problem, linearAnalyzer);

            staticAnalyzer.Initialize();
            staticAnalyzer.Solve();

            Console.WriteLine("Analysis finished");
            return solver.LinearSystems[0].Solution;
        }
    }
}
