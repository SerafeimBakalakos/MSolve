using ISAAR.MSolve.Analyzers.Optimization.Problems;
using System;

namespace ISAAR.MSolve.SamplesConsole.Optimization.BenchmarkFunctions
{
    /// <summary>
    /// Class for the Eggholder's optimization problem.
    /// <see href="https://en.wikipedia.org/wiki/Test_functions_for_optimization">Wikipedia: Test functions for optimization</see>
    /// </summary>
    public class Eggholder : OptimizationProblem
    {
        public Eggholder()
        {
            this.Dimension = 2;
            this.LowerBound = new double[] { -512, -512 };
            this.UpperBound = new double[] { 512, 512 };
            this.ObjectiveFunction = new Objective();
        }

        class Objective : IObjectiveFunction
        {
            public double Evaluate(double[] x)
            {
                return -(x[1] + 47) * Math.Sin(Math.Sqrt(Math.Abs(x[0]/2 + (x[1] + 47)))) - 
                    x[0] * Math.Sin(Math.Sqrt(Math.Abs(x[0] - (x[1] + 47))));
            }
        }
    }
}
