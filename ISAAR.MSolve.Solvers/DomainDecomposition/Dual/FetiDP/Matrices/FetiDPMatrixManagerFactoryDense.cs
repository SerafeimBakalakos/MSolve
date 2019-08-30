using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.Matrices
{
    public class FetiDPMatrixManagerFactoryDense : IFetiDPMatrixManagerFactory
    {
        public IFetiDPGlobalMatrixManager CreateGlobalMatrixManager(IModel model) 
            => new FetiDPGlobalMatrixManagerDense(model);

        public IFetiDPSubdomainMatrixManager CreateSubdomainMatrixManager(ISubdomain subdomain)
            => new FetiDPSubdomainMatrixManagerDense(subdomain);
    }
}
