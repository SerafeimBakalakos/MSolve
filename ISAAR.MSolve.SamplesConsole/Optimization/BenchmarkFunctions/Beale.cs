using ISAAR.MSolve.Analyzers.Optimization;
using System;

namespace ISAAR.MSolve.SamplesConsole.Optimization.BenchmarkFunctions
{
    /// <summary>
    ///     Class for the Beale's function.
    ///         
    ///     <see href="https://en.wikipedia.org/wiki/Test_functions_for_optimization">Wikipedia: Test functions for optimization</see>
    /// </summary>
    public class Beale:IObjectiveFunction

    {
        private static int DIMENSION = 2;

        public double Fitness(double[] x)
        {
            return Math.Pow((1.5 - x[0] + x[0] * x[1]), 2) + Math.Pow((2.25 - x[0] + x[0] * x[1] * x[1]), 2)
                    + Math.Pow((2.625 - x[0] + x[0] * x[1] * x[1] * x[1]), 2);
        }

        public int Dimension()
        {
            return DIMENSION;
        }
    }

}
