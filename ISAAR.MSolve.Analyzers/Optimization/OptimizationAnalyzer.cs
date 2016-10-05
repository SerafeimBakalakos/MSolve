using ISAAR.MSolve.Analyzers.Optimization.Problems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ISAAR.MSolve.Analyzers.Optimization
{
    public class OptimizationAnalyzer : IOptimizationAnalyzer
    {
        public OptimizationProblem optimizationProblem;
        public IOptimizationAlgorithm optimizationAlgorithm;

        public OptimizationAnalyzer(IOptimizationAlgorithm optimizationAlgorithm)
        {
            this.optimizationAlgorithm = optimizationAlgorithm;
        }

        void IOptimizationAnalyzer.Optimize()
        {
            optimizationAlgorithm.Solve();
        }
    }
}
