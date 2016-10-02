using ISAAR.MSolve.Analyzers.Optimization;
using System;

namespace ISAAR.MSolve.SamplesConsole.Optimization.BenchmarkFunctions
{
    /// <summary>
    ///     Class for the McCormick's function.
    ///         
    ///     <see href="https://en.wikipedia.org/wiki/Test_functions_for_optimization">Wikipedia: Test functions for optimization</see>
    /// </summary>
    class McCormick : IObjectiveFunction
        {
            private static int DIMENSION = 2;

            public double Fitness(double[] x)
            {
                return Math.Sin(x[0] + x[1]) + Math.Pow(x[0] - x[1], 2) - 1.5 * x[0] + 2.5 * x[1] + 1;
            }

            public int Dimension()
            {
                return DIMENSION;
            }
        }
    }
