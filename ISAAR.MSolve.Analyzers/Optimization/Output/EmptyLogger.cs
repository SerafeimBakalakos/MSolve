using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISAAR.MSolve.Analyzers.Optimization.Output
{
    class EmptyLogger : IOptimizationLogger
    {
        public void Log(IOptimizationAlgorithm algorithm)
        {
        }

        public void PrintToConsole()
        {
        }
    }
}
