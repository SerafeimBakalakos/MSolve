using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Iterative.Preconditioning;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.GsiFetiDP
{
    public class GsiFetiDPPreconditioner : IPreconditioner
    {
        private readonly FetiDP3dSolverSerial fetiDP;
        private readonly IModel model;

        public GsiFetiDPPreconditioner(IModel model, FetiDP3dSolverSerial fetiDP)
        {
            this.model = model;
            this.fetiDP = fetiDP;
        }

        public void SolveLinearSystem(IVectorView rhsVector, IVector lhsVector)
        {
            // Create the rhs vectors of each subdomain
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                var subdomainRhs = fetiDP.GetLinearSystem(subdomain).RhsVector;
                model.GlobalDofOrdering.ExtractVectorSubdomainFromGlobal(subdomain, rhsVector, subdomainRhs);
                DistributeForceVector(subdomain, subdomainRhs);
            }

            // Use FETI-DP as preconditioner
            fetiDP.Solve();

            Debug.WriteLine("FETI-DP used as preconditioner for GSI. PCG iterations for current residual = " 
                + fetiDP.Logger.GetNumIterationsOfIterativeAlgorithm(fetiDP.Logger.CurrentStep - 1));

            // Assemble the results into a global vector
            lhsVector.CopyFrom(fetiDP.GatherGlobalDisplacements());
        }

        public void Update()
        {

        }

        private void DistributeForceVector(ISubdomain subdomain, IVector forceVector)
        {
            foreach ((INode node, IDofType dof, int idx) in subdomain.FreeDofOrdering.FreeDofs)
            {
                forceVector.Set(idx, forceVector[idx] / node.Multiplicity);
            }
        }
    }
}
