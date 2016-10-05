using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ISAAR.MSolve.Analyzers.Optimization.Output
{
    public interface IOptimizationLogger
    {
        void Log(IOptimizationAlgorithm algorithm);
    }
}
