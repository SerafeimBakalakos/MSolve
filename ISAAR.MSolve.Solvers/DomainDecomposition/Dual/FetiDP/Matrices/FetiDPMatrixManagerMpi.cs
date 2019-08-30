using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.Ordering.Reordering;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.Matrices
{
    public class FetiDPMatrixManagerMpi : IFetiDPMatrixManager
    {
        private readonly IFetiDPGlobalMatrixManager matrixManagerGlobal_master;
        private readonly IFetiDPSubdomainMatrixManager matrixManagerSubdomain;
        private readonly IModel model;
        private readonly ProcessDistribution procs;
        private readonly ISubdomain subdomainOfProcess;

        public FetiDPMatrixManagerMpi(ProcessDistribution processDistribution, IModel model, 
            IFetiDPMatrixManagerFactory matrixManagerFactory)
        {
            this.procs = processDistribution;
            this.model = model;
            this.subdomainOfProcess = model.GetSubdomain(processDistribution.OwnSubdomainID);

            this.matrixManagerSubdomain = matrixManagerFactory.CreateSubdomainMatrixManager(subdomainOfProcess);
            if (processDistribution.IsMasterProcess)
            {
                matrixManagerGlobal_master = matrixManagerFactory.CreateGlobalMatrixManager(model);
            }
        }

        public IFetiDPGlobalMatrixManager GlobalMatrixManager
        {
            get
            {
                procs.CheckProcessIsMaster();
                return matrixManagerGlobal_master;
            }
        }

        public IFetiDPSubdomainMatrixManager GetSubdomainMatrixManager(ISubdomain subdomain)
        {
            procs.CheckProcessMatchesSubdomain(subdomain.ID);
            return matrixManagerSubdomain;
        }

        public DofPermutation ReorderGlobalCornerDofs(IFetiDPDofSeparator dofSeparator)
        {
            procs.CheckProcessIsMaster();
            return matrixManagerGlobal_master.ReorderCornerDofs(dofSeparator);
        }

        public DofPermutation ReorderSubdomainInternalDofs(ISubdomain subdomain, IFetiDPDofSeparator dofSeparator)
        {
            procs.CheckProcessMatchesSubdomain(subdomain.ID);
            return matrixManagerSubdomain.ReorderInternalDofs(subdomain, dofSeparator);
        }

        public DofPermutation ReorderSubdomainRemainderDofs(ISubdomain subdomain, IFetiDPDofSeparator dofSeparator)
        {
            procs.CheckProcessMatchesSubdomain(subdomain.ID);
            return matrixManagerSubdomain.ReorderRemainderDofs(subdomain, dofSeparator);
        }
    }
}
