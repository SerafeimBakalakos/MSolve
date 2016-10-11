using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISAAR.MSolve.Analyzers.Optimization.Logging
{
    public class NoLogger : IOptimizationLogger
    {
        public void Log(IOptimizationAlgorithm algorithm)
        {
        }

        public void PrintToConsole()
        {
        }
    }
}
