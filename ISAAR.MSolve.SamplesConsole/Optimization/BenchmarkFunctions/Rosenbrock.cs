using ISAAR.MSolve.Analyzers.Optimization.Problems;
using System.Linq;

namespace ISAAR.MSolve.SamplesConsole.Optimization.BenchmarkFunctions
{
    /// <summary>
    /// Class for the Rosenbrock's optimization problem.
    /// <see href="https://en.wikipedia.org/wiki/Test_functions_for_optimization">Wikipedia: Test functions for optimization</see>
    /// </summary>
    public class Rosenbrock : OptimizationProblem
    {
        public Rosenbrock()
        {
            this.Dimension = 10;

            this.LowerBound = new double[Dimension];
            LowerBound = LowerBound.Select(i => -10.0).ToArray();

            this.UpperBound = new double[Dimension];
            UpperBound = UpperBound.Select(i => 10.0).ToArray();

            this.ObjectiveFunction = new Objective();
        }

        class Objective : IObjectiveFunction
        {
            public double Evaluate(double[] x)
            {
                double f = 0.0;
                double t1;
                double t2;

                for (int i = 0; i < (x.Length - 1); i++)
                {
                    t1 = (x[i + 1] - x[i] * x[i]) * (x[i + 1] - x[i] * x[i]);
                    t2 = (1 - x[i]) * (1 - x[i]);
                    f += 100.0 * t1 + t2;
                }
                return f;
            }
        }
    }
}