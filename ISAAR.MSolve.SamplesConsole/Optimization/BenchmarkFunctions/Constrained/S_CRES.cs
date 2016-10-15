using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Analyzers.Optimization.Problem;

namespace ISAAR.MSolve.SamplesConsole.Optimization.BenchmarkFunctions.Constrained
{
    class S_CRES : OptimizationProblem
    {
        public S_CRES()
        {
            this.Dimension = 2;
            this.LowerBound = new double[] { 0, 0 };
            this.UpperBound = new double[] { 6, 6 };
            this.ObjectiveFunction = new Objective();
            this.InequalityConstraints = new IConstraintFunction[] { new G0(), new G1()};
        }

        class Objective : IObjectiveFunction
        {
            public double Evaluate(double[] x)
            {
                double x1 = x[0];
                double x2 = x[1];
                double x1_2 = Math.Pow(x1, 2.0);
                double x2_2 = Math.Pow(x2, 2.0);
                return Math.Pow(x1_2 + x2 - 11, 2) + Math.Pow(x1 + x2_2 - 7, 2);
            }
        }

        class G0 : IConstraintFunction
        {
            public double Evaluate(double[] x)
            {
                return 4.84 - Math.Pow(x[0] - 0.05, 2) - Math.Pow(x[1] - 2.5, 2);
            }
        }

        class G1 : IConstraintFunction
        {
            public double Evaluate(double[] x)
            {
                return  Math.Pow(x[0], 2) + Math.Pow(x[1] - 2.5, 2) - 4.84;
            }
        }
    }

}
