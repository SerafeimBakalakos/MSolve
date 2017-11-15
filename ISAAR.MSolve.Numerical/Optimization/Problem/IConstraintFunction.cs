using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ISAAR.MSolve.Numerical.Optimization.Problem
{

    public interface IConstraintFunction
    {
        double Evaluate(double[] x);
    }
}
