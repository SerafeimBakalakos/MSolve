using ISAAR.MSolve.Analyzers.Optimization;
using System;

namespace ISAAR.MSolve.SamplesConsole.Optimization.BenchmarkFunctions
{
    /// <summary>
    ///     Class for the McCormick's function.
    ///         
    ///     <see href="https://en.wikipedia.org/wiki/Test_functions_for_optimization">Wikipedia: Test functions for optimization</see>
    /// </summary>
    public class McCormick : OptimizationProblem
    {
        public McCormick()
        {
            this.Dimension = 2;
            this.LowerBound = new double[] { -1.5, -4.0 };
            this.UpperBound = new double[] { 3.0, 4.0 };
            this.ObjectiveFunction = new Objective();
        }

        class Objective : IObjectiveFunction
        {
            public double Fitness(double[] x)
            {
                return Math.Sin(x[0] + x[1]) + Math.Pow(x[0] - x[1], 2) - 1.5 * x[0] + 2.5 * x[1] + 1;
            }
        }
    }
}