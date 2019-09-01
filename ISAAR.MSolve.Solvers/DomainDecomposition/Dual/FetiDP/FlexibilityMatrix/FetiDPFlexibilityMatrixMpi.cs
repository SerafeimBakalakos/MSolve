using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using MPI;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.FlexibilityMatrix
{
    public class FetiDPFlexibilityMatrixMpi : FetiDPFlexibilityMatrixBase
    {
        private readonly ProcessDistribution procs;
        private readonly FetiDPSubdomainFlexibilityMatrix subdomainFlexibility;

        public FetiDPFlexibilityMatrixMpi(ProcessDistribution procs, IModel model, IFetiDPDofSeparator dofSeparator, 
            ILagrangeMultipliersEnumerator lagrangeEnumerator, IFetiDPMatrixManager matrixManager) 
            : base(dofSeparator, lagrangeEnumerator)
        {
            this.procs = procs;

            ISubdomain subdomain = model.GetSubdomain(procs.OwnSubdomainID);
            this.subdomainFlexibility = new FetiDPSubdomainFlexibilityMatrix(subdomain, dofSeparator, lagrangeEnumerator,
                matrixManager.GetSubdomainMatrixManager(subdomain));
        }

        protected override void SumSubdomainContributions(Vector lhs, Vector rhs, CheckInput checkInput, 
            CalcSubdomainContribution calcSubdomainContribution)
        {
            if (procs.IsMasterProcess) checkInput(lhs, rhs);
            BroadcastLhs(ref lhs);
            Vector subdomainRhs = calcSubdomainContribution(subdomainFlexibility, lhs);
            ReduceRhs(subdomainRhs, rhs);
        }

        private void BroadcastLhs(ref Vector lhs)
        {
            //TODO: Use a dedicated class for MPI communication of Vector. This class belongs to a project LinearAlgebra.MPI.
            //      Avoid copying the array.
            double[] lhsArray = null;
            if (procs.IsMasterProcess) lhsArray = lhs.CopyToArray();
            procs.Communicator.Broadcast<double>(ref lhsArray, procs.MasterProcess);
            lhs = Vector.CreateFromArray(lhsArray);
        }

        private void ReduceRhs(Vector subdomainRhs, Vector globalRhs)
        {
            double[] rhsArray = subdomainRhs.CopyToArray();
            double[] sum = procs.Communicator.Reduce<double>(rhsArray, Operation<double>.Add, procs.MasterProcess);
            if (procs.IsMasterProcess) globalRhs.CopyFrom(Vector.CreateFromArray(sum));
        }
    }
}
