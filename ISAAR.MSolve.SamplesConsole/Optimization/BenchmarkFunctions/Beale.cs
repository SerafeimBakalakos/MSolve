using ISAAR.MSolve.Analyzers.Optimization.Problems;
using System;

namespace ISAAR.MSolve.SamplesConsole.Optimization.BenchmarkFunctions
{
    /// <summary>
    ///     Class for the Beale's function.
    ///         
    ///     <see href="https://en.wikipedia.org/wiki/Test_functions_for_optimization">Wikipedia: Test functions for optimization</see>
    /// </summary>
    public class Beale : OptimizationProblem
    {
        public Beale()
        {
            this.Dimension = 2;
            this.LowerBound = new double[] { -4.5, -4.5 };
            this.UpperBound = new double[] { 4.5, 4.5 };
            this.ObjectiveFunction = new Objective();
        }

        class Objective : IObjectiveFunction
        {
            public double Evaluate(double[] x)
            {
                return Math.Pow((1.5 - x[0] + x[0] * x[1]), 2) + Math.Pow((2.25 - x[0] + x[0] * x[1] * x[1]), 2)
                    + Math.Pow((2.625 - x[0] + x[0] * x[1] * x[1] * x[1]), 2);
            }
        }
    }
}
