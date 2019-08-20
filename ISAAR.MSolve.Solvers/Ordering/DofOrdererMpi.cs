using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.Reordering;
using ISAAR.MSolve.Solvers.Ordering.Reordering;
using MPI;

//TODO: The solver should decide which subdomains will be reused. This class only provides functionality.
namespace ISAAR.MSolve.Solvers.Ordering
{
    /// <summary>
    /// Orders the unconstrained freedom degrees of each subdomain and the shole model. Also applies any reordering and other 
    /// optimizations.
    /// </summary>
    public class DofOrdererMpi : DofOrdererBase
    {
        //TODO: this should also be a strategy, so that I could have caching with fallbacks, in case of insufficient memor.
        private readonly Intracommunicator comm;
        private readonly IDofSerializer dofSerializer;
        private readonly Dictionary<int, INode> globalNodes_master;
        private readonly int masterProcess; 
        private readonly ProcessDistribution processDistribution;
        private readonly int rank;
        private readonly ISubdomain subdomain;

        public DofOrdererMpi(IFreeDofOrderingStrategy freeOrderingStrategy, IDofReorderingStrategy reorderingStrategy, 
            Intracommunicator comm, int masterProcess, ProcessDistribution processDistribution, IDofSerializer dofSerializer,
            Dictionary<int, INode> globalNodes, ISubdomain subdomain, bool cacheElementToSubdomainDofMaps = true):
            base(freeOrderingStrategy, reorderingStrategy, cacheElementToSubdomainDofMaps)
        {
            this.comm = comm;
            this.rank = comm.Rank;
            this.masterProcess = masterProcess;
            this.processDistribution = processDistribution;
            this.dofSerializer = dofSerializer;
            this.globalNodes_master = globalNodes;
            this.subdomain = subdomain;
        }

        public override void OrderFreeDofs(IStructuralModel model)
        {
            // Each process orders its subdomain dofs
            ISubdomainFreeDofOrdering subdomainOrdering = OrderFreeDofs(subdomain);
            subdomain.FreeDofOrdering = subdomainOrdering;

            // Order global dofs
            int numGlobalFreeDofs = -1;
            DofTable globalFreeDofs = null;
            if (rank == masterProcess) (numGlobalFreeDofs, globalFreeDofs) = freeOrderingStrategy.OrderGlobalDofs(model);
            var globalOrdering = new GlobalFreeDofOrderingMpi(numGlobalFreeDofs, globalFreeDofs, globalNodes_master, subdomain, 
                comm, masterProcess, processDistribution, dofSerializer);
            globalOrdering.CreateSubdomainGlobalMaps(model);
            model.GlobalDofOrdering = globalOrdering;
        }
    }
}
