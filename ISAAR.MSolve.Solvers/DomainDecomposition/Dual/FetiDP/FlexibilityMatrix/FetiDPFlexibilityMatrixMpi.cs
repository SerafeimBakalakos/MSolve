using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.MPI;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.FlexibilityMatrix
{
    public class FetiDPFlexibilityMatrixMpi : IFetiDPFlexibilityMatrix
    {
        private readonly IFetiDPDofSeparator dofSeparator;
        private readonly ILagrangeMultipliersEnumerator lagrangesEnumerator;
        private readonly IModel model;
        private readonly ProcessDistribution procs;
        private readonly IFetiDPSubdomainFlexibilityMatrix subdomainFlexibility;

        public FetiDPFlexibilityMatrixMpi(ProcessDistribution procs, IModel model, IFetiDPDofSeparator dofSeparator, 
            ILagrangeMultipliersEnumerator lagrangesEnumerator, IFetiDPMatrixManager matrixManager) 
        {
            this.procs = procs;
            this.model = model;
            this.dofSeparator = dofSeparator;
            this.lagrangesEnumerator = lagrangesEnumerator;
            this.subdomainFlexibility = new FetiDPSubdomainFlexibilityMatrix(model.GetSubdomain(procs.OwnSubdomainID), 
                dofSeparator, lagrangesEnumerator, matrixManager);
            this.NumGlobalLagrangeMultipliers = lagrangesEnumerator.NumLagrangeMultipliers;
        }

        public int NumGlobalLagrangeMultipliers { get; }

        public Vector MultiplyGlobalFIrc(Vector vIn)
        {
            if (procs.IsMasterProcess)
            {
                FetiDPFlexibilityMatrixUtilities.CheckMultiplicationGlobalFIrc(vIn, dofSeparator, lagrangesEnumerator);
            }
            procs.Communicator.BroadcastVector(ref vIn, dofSeparator.NumGlobalCornerDofs, procs.MasterProcess);
            Vector subdomainRhs = subdomainFlexibility.MultiplySubdomainFIrc(vIn);
            return procs.Communicator.SumVector(subdomainRhs, procs.MasterProcess);
        }

        public Vector MultiplyGlobalFIrcTransposed(Vector vIn)
        {
            if (procs.IsMasterProcess)
            {
                FetiDPFlexibilityMatrixUtilities.CheckMultiplicationGlobalFIrcTransposed(vIn, dofSeparator, lagrangesEnumerator);
            }
            procs.Communicator.BroadcastVector(ref vIn, lagrangesEnumerator.NumLagrangeMultipliers, procs.MasterProcess);
            Vector subdomainRhs = subdomainFlexibility.MultiplySubdomainFIrcTransposed(vIn);
            return procs.Communicator.SumVector(subdomainRhs, procs.MasterProcess);
        }

        public void MultiplyGlobalFIrr(Vector vIn, Vector vOut)
        {
            if (procs.IsMasterProcess)
            {
                FetiDPFlexibilityMatrixUtilities.CheckMultiplicationGlobalFIrr(vIn, vOut, lagrangesEnumerator);
            }
            procs.Communicator.BroadcastVector(ref vIn, lagrangesEnumerator.NumLagrangeMultipliers, procs.MasterProcess);
            Vector subdomainRhs = subdomainFlexibility.MultiplySubdomainFIrr(vIn);
            procs.Communicator.SumVector(subdomainRhs, vOut, procs.MasterProcess);
        }
    }
}
