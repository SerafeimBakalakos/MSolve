using System;
using ISAAR.MSolve.Analyzers.Optimization.Benchmarks.ProblemTypes;

namespace ISAAR.MSolve.SamplesConsole.Optimization.BenchmarkFunctions
{
    /// <summary>
    /// Class for the Ackley's optimization problem.
    /// <see href="https://en.wikipedia.org/wiki/Test_functions_for_optimization">Wikipedia: Test functions for optimization</see>
    /// </summary>
    public class Ackley : SingleObjectiveUnconstrained
    {
        public Ackley()
        {
            this.Dimension = 2;
            this.LowerBound = new double[] { -5, -5 };
            this.UpperBound = new double[] { 5, 5 };
            this.ObjectiveFunction = (x) =>
                    -20 * Math.Exp(-0.2 * Math.Sqrt(0.5 * (Math.Pow(x[0], 2) + Math.Pow(x[1], 2)))) -
                    Math.Exp(0.5 * (Math.Cos(2 * Math.PI * x[0]) + Math.Cos(2 * Math.PI * x[1]))) + Math.E + 20;
        }
    }
}