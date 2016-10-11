using ISAAR.MSolve.Analyzers.Optimization.Problem;
using System;

namespace ISAAR.MSolve.SamplesConsole.Optimization.BenchmarkFunctions
{
    /// <summary>
    /// Class for the Ackley's optimization problem.
    /// <see href="https://en.wikipedia.org/wiki/Test_functions_for_optimization">Wikipedia: Test functions for optimization</see>
    /// </summary>
    public class Ackley : OptimizationProblem
    {
        //public static OptimizationProblem CreateProblem()
        //{
        //    OptimizationProblem problem = new OptimizationProblem();
        //    problem.Dimension = 2;
        //    problem.LowerBound = new double[] { -5, -5 };
        //    problem.UpperBound = new double[] { 5, 5 };
        //    problem.ObjectiveFunction = new ObjectiveFunction();

        //    return problem;
        //}

        public Ackley()
        {
            this.Dimension = 2;
            this.LowerBound = new double[] { -5, -5 };
            this.UpperBound = new double[] { 5, 5 };
            this.ObjectiveFunction = new Objective();
        }

        class Objective : IObjectiveFunction
        {
            public double Evaluate(double[] x)
            {
                return -20 * Math.Exp(-0.2 * Math.Sqrt(0.5 * (Math.Pow(x[0], 2) + Math.Pow(x[1], 2)))) -
                    Math.Exp(0.5 * (Math.Cos(2 * Math.PI * x[0]) + Math.Cos(2 * Math.PI * x[1]))) + Math.E + 20;
            }
        }
    }
}