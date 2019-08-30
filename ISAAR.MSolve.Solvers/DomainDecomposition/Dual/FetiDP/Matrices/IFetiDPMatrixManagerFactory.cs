using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.InterfaceProblem;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.Matrices
{
    public interface IFetiDPMatrixManagerFactory
    {
        IFetiDPGlobalMatrixManager CreateGlobalMatrixManager(IModel model); 
        IFetiDPSubdomainMatrixManager CreateSubdomainMatrixManager(ISubdomain subdomain);
    }
}
