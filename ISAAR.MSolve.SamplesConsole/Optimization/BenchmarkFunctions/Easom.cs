using ISAAR.MSolve.Analyzers.Optimization.Problem;
using System;

namespace ISAAR.MSolve.SamplesConsole.Optimization.BenchmarkFunctions
{
    /// <summary>
    /// Class for the Easom's optimization problem.
    /// <see href="https://en.wikipedia.org/wiki/Test_functions_for_optimization">Wikipedia: Test functions for optimization</see>
    /// </summary>
    public class Easom : OptimizationProblem
    {
        public Easom()
        {
            this.Dimension = 2;
            this.LowerBound = new double[] { -100, -100 };
            this.UpperBound = new double[] { 100, 100 };
            this.ObjectiveFunction = new Objective();
        }

        class Objective : IObjectiveFunction
        {
            public double Evaluate(double[] x)
            {
                return -Math.Cos(x[0]) * Math.Cos(x[1]) * 
                    Math.Exp(-((Math.Pow(x[0] - Math.PI, 2) + Math.Pow(x[1] - Math.PI, 2))));
            }
        }
    }
}
