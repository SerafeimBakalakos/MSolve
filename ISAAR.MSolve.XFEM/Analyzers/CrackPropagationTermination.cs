using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.XFEM_OLD.Analyzers
{
    public enum CrackPropagationTermination
    {
        RequiredIterationsWereCompleted, CrackExitsDomainBoundary, MechanismIsCreated, FractureToughnessIsExceeded
    }
}
