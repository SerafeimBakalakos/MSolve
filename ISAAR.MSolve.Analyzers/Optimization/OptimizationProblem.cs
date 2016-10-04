using System;

namespace ISAAR.MSolve.Analyzers.Optimization
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

        public void CheckInput()
        {
           if ((LowerBound.Length != UpperBound.Length) && (UpperBound.Length != Dimension))
           {
                throw new ArgumentException("Dimension, Lower and Upper bounds lengths must be equal!");
           }
            for (int i = 0; i < LowerBound.Length; i++)
            {
                if (LowerBound[i] >= UpperBound[i])
                {
                    throw new ArgumentException("Lower bound value of design variable " + i 
                        + " is greater/equal of the corresponding upper bound!");
                }
            }
        }
    }
}