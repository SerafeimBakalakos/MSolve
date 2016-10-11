using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ISAAR.MSolve.Analyzers.Optimization.Logging
{
    public interface IOptimizationLogger
    {
        void Log(IOptimizationAlgorithm algorithm);
    }
}
