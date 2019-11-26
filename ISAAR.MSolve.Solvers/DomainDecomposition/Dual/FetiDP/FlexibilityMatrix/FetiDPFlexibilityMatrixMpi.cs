using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Distributed;
using ISAAR.MSolve.LinearAlgebra.Distributed.Vectors;
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
        private readonly Dictionary<ISubdomain, IFetiDPSubdomainFlexibilityMatrix> subdomainFlexibilities;

        public FetiDPFlexibilityMatrixMpi(ProcessDistribution procs, IModel model, IFetiDPDofSeparator dofSeparator,
            ILagrangeMultipliersEnumerator lagrangesEnumerator, IFetiDPMatrixManager matrixManager)
        {
            this.procs = procs;
            this.model = model;
            this.dofSeparator = dofSeparator;
            this.lagrangesEnumerator = lagrangesEnumerator;
            this.NumGlobalLagrangeMultipliers = lagrangesEnumerator.NumLagrangeMultipliers;

            this.subdomainFlexibilities = new Dictionary<ISubdomain, IFetiDPSubdomainFlexibilityMatrix>();
            foreach (int s in procs.GetSubdomainIdsOfProcess(procs.OwnRank))
            {
                ISubdomain subdomain = model.GetSubdomain(s);
                this.subdomainFlexibilities[subdomain] =
                    new FetiDPSubdomainFlexibilityMatrix(subdomain, dofSeparator, lagrangesEnumerator, matrixManager);
            }
        }

        public int NumGlobalLagrangeMultipliers { get; }

        public Vector MultiplyGlobalFIrc(Vector vIn)
        {
            if (procs.IsMasterProcess)
            {
                FetiDPFlexibilityMatrixUtilities.CheckMultiplicationGlobalFIrc(vIn, dofSeparator, lagrangesEnumerator);
            }
            var transferrer = new VectorTransferrer(procs);
            transferrer.BroadcastVector(ref vIn, dofSeparator.NumGlobalCornerDofs);
            IEnumerable<Vector> subdomainRhs = subdomainFlexibilities.Values.Select(F => F.MultiplySubdomainFIrc(vIn));
            return transferrer.SumVectors(subdomainRhs);
        }

        public Vector MultiplyGlobalFIrcTransposed(Vector vIn)
        {
            if (procs.IsMasterProcess)
            {
                FetiDPFlexibilityMatrixUtilities.CheckMultiplicationGlobalFIrcTransposed(vIn, dofSeparator, lagrangesEnumerator);
            }
            var transferrer = new VectorTransferrer(procs);
            transferrer.BroadcastVector(ref vIn, lagrangesEnumerator.NumLagrangeMultipliers);
            IEnumerable<Vector> subdomainRhs = subdomainFlexibilities.Values.Select(F => F.MultiplySubdomainFIrcTransposed(vIn));
            return transferrer.SumVectors(subdomainRhs);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vIn"></param>
        /// <param name="vOut">It will be ignored in processes other than master.</param>
        public void MultiplyGlobalFIrr(Vector vIn, Vector vOut)
        {
            if (procs.IsMasterProcess)
            {
                FetiDPFlexibilityMatrixUtilities.CheckMultiplicationGlobalFIrr(vIn, vOut, lagrangesEnumerator);
            }
            var transferrer = new VectorTransferrer(procs);
            transferrer.BroadcastVector(ref vIn, lagrangesEnumerator.NumLagrangeMultipliers);
            IEnumerable<Vector> subdomainRhs = subdomainFlexibilities.Values.Select(F => F.MultiplySubdomainFIrr(vIn));
            transferrer.SumVectors(subdomainRhs, vOut);
        }
    }
}
