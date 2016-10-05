﻿using ISAAR.MSolve.Analyzers.Optimization.Problems;
using System;

namespace ISAAR.MSolve.SamplesConsole.Optimization.BenchmarkFunctions
{
    /// <summary>
    ///     Class for the Goldstein-Price's function.
    ///         
    ///     <see href="https://en.wikipedia.org/wiki/Test_functions_for_optimization">Wikipedia: Test functions for optimization</see>
    /// </summary>
    public class GoldsteinPrice : OptimizationProblem
    {
        public GoldsteinPrice()
        {
            this.Dimension = 2;
            this.LowerBound = new double[] { -2, -2 };
            this.UpperBound = new double[] { 2, 2 };
            this.ObjectiveFunction = new Objective();
        }

        class Objective : IObjectiveFunction
        {
            public double Evaluate(double[] x)
            {
                return (1 + Math.Pow(x[0] + x[1] + 1, 2) *
                   (19 - 14 * x[0] + 3 * Math.Pow(x[0], 2) - 14 * x[1] + 6 * x[0] * x[1] + 3 * Math.Pow(x[1], 2))) *
                   (30 + Math.Pow(2 * x[0] - 3 * x[1], 2) * (18 - 32 * x[0] + 12 * Math.Pow(x[0], 2) + 48 * x[1] - 36 * x[0] * x[1] + 27 * Math.Pow(x[1], 2)));
            }
        }
    }
}