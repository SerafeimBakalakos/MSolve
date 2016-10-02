using ISAAR.MSolve.Analyzers.Optimization;
using System;

namespace ISAAR.MSolve.SamplesConsole.Optimization.BenchmarkFunctions
{
    /// <summary>
    ///     Class for the Ackley's function.
    ///         
    ///     <see href="https://en.wikipedia.org/wiki/Test_functions_for_optimization">Wikipedia: Test functions for optimization</see>
    /// </summary>
    class Ackley : IObjectiveFunction
    {
        private static int DIMENSION = 2;

        public double Fitness(double[] x)
        {
            return -20*Math.Exp(-0.2*Math.Sqrt(0.5*(Math.Pow(x[0],2) + Math.Pow(x[1], 2)))) -
                Math.Exp(0.5 * (Math.Cos(2 * Math.PI * x[0]) + Math.Cos(2 * Math.PI * x[1]))) + Math.E + 20;
        }

        public int Dimension()
        {
            return DIMENSION;
        }
    }
}
