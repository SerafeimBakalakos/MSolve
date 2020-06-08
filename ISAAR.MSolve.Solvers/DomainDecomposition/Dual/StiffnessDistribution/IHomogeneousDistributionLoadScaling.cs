using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.StiffnessDistribution
{
    public interface IHomogeneousDistributionLoadScaling
    {
        double ScaleNodalLoad(ISubdomain subdomain, INodalLoad load);
        void ScaleForceVectorFree(ISubdomain subdomain, Vector forceVector);
    }
}
