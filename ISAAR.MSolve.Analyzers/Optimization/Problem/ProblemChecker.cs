using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ISAAR.MSolve.Analyzers.Optimization.Problem
{
    static class ProblemChecker
    {
        public static void Check(OptimizationProblem problem)
        {
            CheckDesignVariables(problem);
            CheckBounds(problem);
            CheckObjectives(problem);
        }

        private static void CheckDesignVariables(OptimizationProblem problem)
        {
            if (problem.Dimension < 1)
            {
                throw new ArgumentException("The number of continuous design variables must be >= 1, but was : " 
                    + problem.Dimension);
            }
            if (problem.LowerBound.Length != problem.Dimension)
            {
                throw new ArgumentException("There number of continuous lower bounds was " + problem.LowerBound.Length + 
                    " , which is different than the number of continuous design variables " + problem.Dimension 
                    + ". They must be the same");
            }
            if (problem.UpperBound.Length != problem.Dimension)
            {
                throw new ArgumentException("There number of continuous upper bounds was " + problem.LowerBound.Length +
                    " , which is different than the number of continuous design variables " + problem.Dimension
                    + ". They must be the same");
            }
        }

        private static void CheckBounds(OptimizationProblem problem)
        {
            for (int i = 0; i < problem.Dimension; i++)
            {
                if (problem.LowerBound[i] >= problem.UpperBound[i])
                {
                    throw new ArgumentException("Lower bound value of design variable " + i
                        + " must be lower than the corresponding upper bound!");
                }
            }
        }

        private static void CheckObjectives(OptimizationProblem problem)
        {
            if (problem.ObjectiveFunction == null)
            {
                throw new ArgumentException("The objective function must not be null");
            }
        }
    }
}
