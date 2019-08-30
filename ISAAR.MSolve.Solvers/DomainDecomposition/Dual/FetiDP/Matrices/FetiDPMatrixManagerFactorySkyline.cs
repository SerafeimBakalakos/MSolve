using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Reordering;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.Matrices
{
    public class FetiDPMatrixManagerFactorySkyline : IFetiDPMatrixManagerFactory
    {
        private readonly IReorderingAlgorithm reordering;

        public FetiDPMatrixManagerFactorySkyline(IReorderingAlgorithm reordering)
        {
            this.reordering = reordering;
        }

        public IFetiDPGlobalMatrixManager CreateGlobalMatrixManager(IModel model) 
            => new FetiDPGlobalMatrixManagerSkyline(model, reordering);

        public IFetiDPSubdomainMatrixManager CreateSubdomainMatrixManager(ISubdomain subdomain)
            => new FetiDPSubdomainMatrixManagerSkyline(subdomain, reordering);
    }
}
