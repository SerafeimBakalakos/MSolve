using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ISAAR.MSolve.Analyzers.Optimization.Constraints.Penalties
{
    public interface IPenaltyStatic
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fitness"></param>
        /// <param name="x">The design vector</param>
        /// <returns>The penalized value for the specified design</returns>
        double Evaluate(double fitness, double[] x);
    }
}
