using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.Logging.Interfaces
{
    public interface ILogFactory
    {
        IAnalyzerLog[] CreateLogs();
    }
}
