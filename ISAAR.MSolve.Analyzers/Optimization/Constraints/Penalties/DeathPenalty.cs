using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Analyzers.Optimization.Problem;

namespace ISAAR.MSolve.Analyzers.Optimization.Constraints.Penalties
{
    public class DeathPenalty : IPenaltyStatic
    {
        private readonly IConstraintFunction[] inequalityConstraints;

        public DeathPenalty(IConstraintFunction[] inequalityConstraints)
        {
            this.inequalityConstraints = inequalityConstraints;
        }

        public double Evaluate(double fitness, double[] x)
        {
            foreach (var constraint in inequalityConstraints)
            {
                if (constraint.Evaluate(x) < 0) return double.MaxValue;
            }
            return fitness;
        }


    }
}
