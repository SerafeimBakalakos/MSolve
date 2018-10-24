﻿using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.Commons;

namespace ISAAR.MSolve.Analyzers.Interfaces
{
    public interface INonLinearParentAnalyzer_v2 : IAnalyzer_v2
    {
        IVector GetOtherRHSComponents(ILinearSystem_v2 subdomain, IVector currentSolution);
    }
}