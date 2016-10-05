using System;

namespace ISAAR.MSolve.Analyzers.Optimization.Problems
{
    public class OptimizationProblem
    {
        public int Dimension
        {
            get; set;
        }

        public double[] LowerBound
        {
            get; set;
        }

        public double[] UpperBound
        {
            get; set;
        }
        public IObjectiveFunction ObjectiveFunction
        {
            get; set;
        }
    }
}