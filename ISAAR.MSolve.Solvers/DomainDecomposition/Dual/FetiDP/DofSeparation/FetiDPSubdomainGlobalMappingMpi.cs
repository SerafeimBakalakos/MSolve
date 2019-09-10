using System;
using System.Collections.Generic;
using System.Linq;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.StiffnessDistribution;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation
{
    public class FetiDPSubdomainGlobalMappingMpi
    {
        private readonly IStiffnessDistribution distribution;
        private readonly IFetiDPDofSeparator dofSeparator;
        private readonly IModel model;
        private readonly ProcessDistribution procs;

        public FetiDPSubdomainGlobalMappingMpi(ProcessDistribution processDistribution, IModel model,
            IFetiDPDofSeparator dofSeparator, IStiffnessDistribution distribution)
        {
            this.procs = processDistribution;
            this.model = model;
            this.dofSeparator = dofSeparator;
            this.distribution = distribution;
        }

        public double CalcGlobalForcesNorm(Func<ISubdomain, Vector> getSubdomainForces)
        {
            return 10;
            ////TODO: This can be optimized: calculate the dot product f*f for the internal dofs of each subdomain separately,
            ////      only assemble global vector for the boundary dofs, find its dot product with itself, add the contributions
            ////      for the internal dofs and finally apply SQRT(). This would greatly reduce the communication requirements.
            ////TODO: this should be used for non linear analyzers as well (instead of building the global RHS)
            ////TODO: Is this correct? For the residual, it would be wrong to find f-K*u for each subdomain and then call this.

            //Vector globalForces = GatherGlobalForces(getSubdomainForces);
            //double norm = double.NaN;
            //if (procs.IsMasterProcess) norm = globalForces.Norm2();
            //procs.Communicator.Broadcast(ref norm, procs.MasterProcess); //TODO: Not sure if this is needed.
            //return norm;
        }

        public Vector GatherGlobalForces(Func<ISubdomain, Vector> getSubdomainForces)
        {
            model.GlobalDofOrdering.CreateSubdomainGlobalMaps(model);
            Vector subdomainForces = getSubdomainForces(model.GetSubdomain(procs.OwnSubdomainID));
            Vector[] allSubdomainForces = procs.Communicator.Gather(subdomainForces, procs.MasterProcess);
            if (procs.IsMasterProcess)
            {
                var globalForces = Vector.CreateZero(model.GlobalDofOrdering.NumGlobalFreeDofs);
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    ISubdomain subdomain = model.GetSubdomain(procs.GetSubdomainIdOfProcess(p));
                    int[] subdomainFreeToGlobalDofs = model.GlobalDofOrdering.MapSubdomainToGlobalDofs(subdomain);

                    // Internal forces will be copied (which is identical to adding 0 + single value).
                    // Boundary remainder forces will be summed. Previously we had distributed them depending on 
                    // homogeneity / heterogeneity (e.g. Ftot = 0.4 * Ftot + 0.6 * Ftot) and now we sum them. 
                    // Boundary corner forces are also summed. Previously we had also distributed them equally irregardless of 
                    // homogeneity / heterogeneity (e.g. Ftot = 0.5 * Ftot + 0.5 * Ftot) and now we sum them.
                    globalForces.AddIntoThisNonContiguouslyFrom(subdomainFreeToGlobalDofs, allSubdomainForces[p]);
                    //for (int i = 0; i < subdomainForces.Length; ++i) globalForces[subdomainFreeToGlobalDofs[i]] += subdomainForces[i];
                }
                return globalForces;

            }
            else return null;
        }
    }
}
