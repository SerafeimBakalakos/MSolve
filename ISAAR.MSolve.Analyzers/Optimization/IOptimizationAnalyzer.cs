using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ISAAR.MSolve.Analyzers.Optimization
{
    public interface IOptimizationAnalyzer
    {
        /// <summary>
        /// 	Optimizes the current problem.
        /// </summary>
        void Optimize();
    }
}
