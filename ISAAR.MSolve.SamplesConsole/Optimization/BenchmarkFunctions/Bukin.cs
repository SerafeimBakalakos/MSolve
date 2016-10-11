using ISAAR.MSolve.Analyzers.Optimization.Problem;
using System;

namespace ISAAR.MSolve.SamplesConsole.Optimization.BenchmarkFunctions
{
    /// <summary>
    /// Class for the Bukin's optimization problem.
    /// <see href="https://en.wikipedia.org/wiki/Test_functions_for_optimization">Wikipedia: Test functions for optimization</see>
    /// </summary>
    public class Bukin : OptimizationProblem
    {
        public Bukin()
        {
            this.Dimension = 2;
            this.LowerBound = new double[] { -15, -3 };
            this.UpperBound = new double[] { -5, 3 };
            this.ObjectiveFunction = new Objective();
        }

        class Objective : IObjectiveFunction
        {
            public double Evaluate(double[] x)
            {
                return 100 * Math.Sqrt(Math.Abs(x[1] - 0.01 * Math.Pow(x[0], 2))) + 
                    0.01 * Math.Abs(x[0] + 10);
            }
        }
    }
}
