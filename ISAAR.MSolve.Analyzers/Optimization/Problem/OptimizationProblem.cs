using System;

namespace ISAAR.MSolve.Analyzers.Optimization.Problem
{
    public class OptimizationProblem
    {
        private bool isChecked;
        private int dimension;
        private double[] lowerBounds;
        private double[] upperBounds;
        private IObjectiveFunction objectiveFunction;

        public int Dimension
        {
            get { return dimension; }

            set
            {
                dimension = value;
                isChecked = false;
            }
        }

        public double[] LowerBound
        {
            get { return lowerBounds; }

            set
            {
                lowerBounds = value;
                isChecked = false;
            }
        }

        public double[] UpperBound
        {
            get { return upperBounds; }

            set
            {
                upperBounds = value;
                isChecked = false;
            }
        }

        public IObjectiveFunction ObjectiveFunction
        {
            get { return objectiveFunction; }

            set
            {
                objectiveFunction = value;
                isChecked = false;
            }
        }

        public void CheckInput()
        {
            if (!isChecked)
            {
                ProblemChecker.Check(this);
                isChecked = true;
            }
        }
    }
}