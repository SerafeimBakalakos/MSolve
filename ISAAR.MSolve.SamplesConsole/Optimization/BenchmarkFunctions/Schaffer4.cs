using ISAAR.MSolve.Analyzers.Optimization.Problem;
using System;

namespace ISAAR.MSolve.SamplesConsole.Optimization.BenchmarkFunctions
{
    /// <summary>
    /// Class for the Schaffer's No.4 optimization problem.
    /// <see href="https://en.wikipedia.org/wiki/Test_functions_for_optimization">Wikipedia: Test functions for optimization</see>
    /// </summary>
    public class Schaffer4 : OptimizationProblem
    {
        public Schaffer4()
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
                return 0.5 + (Math.Pow(Math.Cos(Math.Sin(Math.Abs(Math.Pow(x[0], 2) - Math.Pow(x[1], 2)))), 2) - 0.5) /
                    Math.Pow(1 + 0.001 * (Math.Pow(x[0], 2) + Math.Pow(x[1], 2)), 2);
            }
        }
    }
}
