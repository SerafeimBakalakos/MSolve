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
            //TODO: This can be optimized: calculate the dot product f*f for the internal dofs of each subdomain separately,
            //      only assemble global vector for the boundary dofs, find its dot product with itself, add the contributions
            //      for the internal dofs and finally apply SQRT(). This would greatly reduce the communication requirements.
            //TODO: this should be used for non linear analyzers as well (instead of building the global RHS)
            //TODO: Is this correct? For the residual, it would be wrong to find f-K*u for each subdomain and then call this.

            Vector globalForces = AssembleSubdomainVectors(getSubdomainForces);
            double norm = double.NaN;
            if (procs.IsMasterProcess) norm = globalForces.Norm2();
            procs.Communicator.Broadcast(ref norm, procs.MasterProcess); //TODO: Not sure if this is needed.
            return norm;
        }

        public Vector GatherGlobalDisplacements(Func<ISubdomain, Vector> getSubdomainFreeDisplacements)
        {
            return AssembleSubdomainVectors(sub =>
            {
                Vector u = getSubdomainFreeDisplacements(sub);
                ScaleSubdomainFreeDisplacements(sub, u);
                return u;
            });
        }

        public Vector GatherGlobalForces(Func<ISubdomain, Vector> getSubdomainForces)
        {
            return AssembleSubdomainVectors(getSubdomainForces);
        }

        private void ScaleSubdomainFreeDisplacements(ISubdomain subdomain, Vector freeDisplacements)
        {
            throw new NotImplementedException();
            //// Boundary remainder dofs: Scale them so that they can be just added at global level
            //int[] remainderToSubdomainDofs = dofSeparator.GetRemainderDofIndices(subdomain);
            //double[] boundaryDofCoeffs = distribution.CalcBoundaryDofCoefficients(subdomain);
            //int[] boundaryDofIndices = dofSeparator.GetBoundaryDofIndices(subdomain);
            //for (int i = 0; i < boundaryDofIndices.Length; ++i)
            //{
            //    int idx = remainderToSubdomainDofs[boundaryDofIndices[i]];
            //    freeDisplacements[idx] *= boundaryDofCoeffs[i];
            //}

            //// Boundary corner dofs: Scale them so that they can be just added at global level
            //int[] cornerToSubdomainDofs = dofSeparator.GetCornerDofIndices(subdomain);
            //double[] cornerDofCoeffs = distribution.CalcCornerDofCoefficients(subdomain);
            //for (int i = 0; i < cornerToSubdomainDofs.Length; ++i)
            //{
            //    int idx = cornerToSubdomainDofs[i];
            //    freeDisplacements[idx] *= cornerDofCoeffs[i];
            //}
        }

        private Vector AssembleSubdomainVectors(Func<ISubdomain, Vector> getSubdomainVector)
        {
            ISubdomain subdomain = model.GetSubdomain(procs.OwnSubdomainID);
            Vector subdomainVector = getSubdomainVector(subdomain);
            Vector globalVector = null;
            if (procs.IsMasterProcess) globalVector = Vector.CreateZero(model.GlobalDofOrdering.NumGlobalFreeDofs);
            model.GlobalDofOrdering.AddVectorSubdomainToGlobal(subdomain, subdomainVector, globalVector);
            return globalVector;
        }
    }
}
