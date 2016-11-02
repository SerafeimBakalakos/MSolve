using System;

namespace ISAAR.MSolve.Analyzers.Optimization.Problem
{
    /// <summary>
    /// Describes the optimization problem to be solved: the objective function(s), the number and bounds of the design 
    /// variables and any constraints. 
    /// </summary>
    public class OptimizationProblem
    {
        private bool isChecked;
        private int dimension;
        private double[] lowerBounds;
        private double[] upperBounds;
        private IObjectiveFunction objectiveFunction;

        /// <summary>
        /// The number of continuous (real) design variables.
        /// </summary>
        public int Dimension
        {
            get { return dimension; }

            set
            {
                dimension = value;
                isChecked = false;
            }
        }

        /// <summary>
        /// A vector containing the minimum alloweable values of the continuous (real) design variables. 
        /// Its length must be equal to <see cref="Dimension"/>. 
        /// To represent unbounded design variables, use <see cref="double.MinValue"/>.
        /// </summary>
        public double[] LowerBound
        {
            get { return lowerBounds; }

            set
            {
                lowerBounds = value;
                isChecked = false;
            }
        }

        /// <summary>
        /// A vector containing the maximum alloweable values of the continuous (real) design variables. 
        /// Its length must be equal to <see cref="Dimension"/>.
        /// To represent unbounded design variables, use <see cref="double.MaxValue"/>.
        /// </summary>
        public double[] UpperBound
        {
            get { return upperBounds; }

            set
            {
                upperBounds = value;
                isChecked = false;
            }
        }

        /// <summary>
        /// The objective function for single objective optimization. All optimization algorithms will try to minimize its value.
        /// Maximization can be performed using -f(x) or 1/f(x) where f(x) is the function that needs to be maximized.
        /// </summary>
        public IObjectiveFunction ObjectiveFunction
        {
            get { return objectiveFunction; }

            set
            {
                objectiveFunction = value;
                isChecked = false;
            }
        }

        /// <summary>
        /// Sanity check for the values of this <see cref="OptimizationProblem"/>'s properties. 
        /// The user does not need to call this method.
        /// </summary>
        public void CheckInput()
        {
            if (!isChecked)
            {
                ProblemChecker.Check(this);
                isChecked = true;
            }
        }

        /// <summary>
        /// Feasible design: g[i](x) &lt;= 0
        /// </summary>
        public IConstraintFunction[] InequalityConstraints { get; set; }

        /// <summary>
        /// Feasible design: h[i](x) = 0
        /// </summary>
        public IConstraintFunction[] EqualityConstraints { get; set; }

    }
}