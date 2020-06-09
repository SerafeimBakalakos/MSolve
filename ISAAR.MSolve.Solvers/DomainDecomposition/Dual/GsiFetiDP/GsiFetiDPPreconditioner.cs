using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Iterative.Preconditioning;
using ISAAR.MSolve.LinearAlgebra.Iterative.Termination;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.InterfaceProblem;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.StiffnessDistribution;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.GsiFetiDP
{
    public class GsiFetiDPPreconditioner : IPreconditioner
    {
        private readonly FetiDP3dSolverSerial fetiDP;
        private readonly IModel model;
        private readonly int maxFetiDPIterations;

        public GsiFetiDPPreconditioner(IModel model, FetiDP3dSolverSerial fetiDP, int maxFetiDPIterations)
        {
            this.model = model;
            this.fetiDP = fetiDP;
            this.maxFetiDPIterations = maxFetiDPIterations;
        }

        public void SolveLinearSystem(IVectorView rhsVector, IVector lhsVector)
        {
            // Create the rhs vectors of each subdomain
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                var subdomainRhs = fetiDP.GetLinearSystem(subdomain).RhsVector;
                model.GlobalDofOrdering.ExtractVectorSubdomainFromGlobal(subdomain, rhsVector, subdomainRhs);
                ScaleForceVector(subdomain, subdomainRhs);
            }

            // Use FETI-DP as preconditioner
            if (fetiDP.InterfaceProblemSolver is FetiDP3dInterfaceProblemSolverReorthogonalizationSerial reorthoInterface)
            {
                //TODO: Allow other components to change parameters in a less hackish way.
                bool checkConvergenceOld = reorthoInterface.CheckConvergence;
                reorthoInterface.CheckConvergence = false;
                IMaxIterationsProvider maxItererationsOld = reorthoInterface.Pcg.MaxIterationsProvider;
                reorthoInterface.Pcg.MaxIterationsProvider = new FixedMaxIterationsProvider(maxFetiDPIterations);
                fetiDP.Solve();
                reorthoInterface.Pcg.MaxIterationsProvider = maxItererationsOld;
                reorthoInterface.CheckConvergence = checkConvergenceOld;
            }
            else fetiDP.Solve();

            Debug.WriteLine("FETI-DP used as preconditioner for GSI. PCG iterations for current residual = " 
                + fetiDP.Logger.GetNumIterationsOfIterativeAlgorithm(fetiDP.Logger.CurrentStep - 1));

            // Assemble the results into a global vector
            lhsVector.CopyFrom(fetiDP.GatherGlobalDisplacements());
        }

        private void ScaleForceVector(ISubdomain subdomain, IVector forceVector)
        {
            fetiDP.StiffnessDistribution.ScaleFreeForceVector(subdomain, (Vector)forceVector);
        }
    }
}
