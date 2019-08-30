using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.Matrices
{
    //This can be done with delegates.
    interface ISubdomainMatrixCalculator
    {
        IMatrixView CalcSubdomainMatrix(ISubdomain subdomain);
        Vector CalcSubdomainVector(ISubdomain subdomain);
    }
}
