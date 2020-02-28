using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Reordering;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.Augmentation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.StiffnessMatrices
{
    public class FetiDP3dMatrixManagerFactorySuiteSparse : IFetiDP3dMatrixManagerFactory
    {
        private readonly IReorderingAlgorithm reordering;

        public FetiDP3dMatrixManagerFactorySuiteSparse(IReorderingAlgorithm reordering)
        {
            this.reordering = reordering;
        }

        public IFetiDP3dGlobalMatrixManager CreateGlobalMatrixManager(IModel model, IFetiDPDofSeparator dofSeparator,
            IAugmentationConstraints augmentationConstraints)
            =>new FetiDP3dGlobalMatrixManagerSkyline(model, dofSeparator, augmentationConstraints, reordering);

        public IFetiDP3dSubdomainMatrixManager CreateSubdomainMatrixManager(ISubdomain subdomain, 
            IFetiDPDofSeparator dofSeparator, ILagrangeMultipliersEnumerator lagrangesEnumerator, 
            IAugmentationConstraints augmentationConstraints)
            => new FetiDP3dSubdomainMatrixManagerSuiteSparse(subdomain, dofSeparator, lagrangesEnumerator, 
                augmentationConstraints, reordering);
    }
}
