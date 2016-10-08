using ISAAR.MSolve.Analyzers.Optimization.Problems;
using System.Linq;

namespace ISAAR.MSolve.SamplesConsole.Optimization.BenchmarkFunctions
{
    /// <summary>
    /// Class for the Sphere's optimization problem.
    /// <see href="https://en.wikipedia.org/wiki/Test_functions_for_optimization">Wikipedia: Test functions for optimization</see>
    /// </summary>
    public class Sphere : OptimizationProblem
    {
        public Sphere()
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

                for (int i = 0; i < x.Length; i++)
                {
                    f += x[i] * x[i];
                }
                return f;
            }
        }
    }
}