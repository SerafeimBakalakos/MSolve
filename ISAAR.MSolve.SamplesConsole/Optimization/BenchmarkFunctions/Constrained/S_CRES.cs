using System;
using ISAAR.MSolve.Analyzers.Optimization.Problem;

namespace ISAAR.MSolve.SamplesConsole.Optimization.StructuralProblems
{
    class S_CRES : DesignProblem
    {
        public S_CRES()
        {
            this.Dimension = 2;
            this.LowerBound = new double[] { 0, 0 };
            this.UpperBound = new double[] { 6, 6 };
            this.DesignFactory = new S_CRESFactory();
        }

        private class S_CRESFactory : IDesignFactory
        {
            public IDesign CreateDesign(double[] x)
            {
                return new S_CRESDesign(x);
            }
        }

        private class S_CRESDesign : IDesign
        {

            public double[] ObjectiveValues { get; }

            public double[] ConstraintValues { get; }

            public S_CRESDesign(double[] x)
            {
                // Get objective value
                this.ObjectiveValues = EvaluateObjective(x);

                // Get constraint values
                this.ConstraintValues = EvaluateConstraints(x);
            }


            private double[] EvaluateObjective(double[] x)
            {
                double x1 = x[0];
                double x2 = x[1];
                double x1_2 = Math.Pow(x1, 2.0);
                double x2_2 = Math.Pow(x2, 2.0);

                double fitness = Math.Pow(x1_2 + x2 - 11, 2) + Math.Pow(x1 + x2_2 - 7, 2);

                return new double[] { fitness };
            }

            private double[] EvaluateConstraints(double[] x)
            {
                double c1 = Math.Pow(x[0] - 0.05, 2) + Math.Pow(x[1] - 2.5, 2) - 4.84;
                double c2 = -Math.Pow(x[0], 2) - Math.Pow(x[1] - 2.5, 2) + 4.84;

                return new double[] { c1, c2 };
            }
        }
    }
}