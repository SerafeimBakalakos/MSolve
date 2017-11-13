using ISAAR.MSolve.Analyzers.Optimization.Problem;
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
        /// <param name="design">The current design</param>
        /// <returns>The penalized value for the specified design</returns>
        double Evaluate(double fitness, IDesign design);
    }
}
