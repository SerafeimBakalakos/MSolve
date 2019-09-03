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
    public class FetiDPFlexibilityMatrixMpi : IFetiDPFlexibilityMatrix
    {
        private readonly IFetiDPDofSeparator dofSeparator;
        private readonly ILagrangeMultipliersEnumerator lagrangesEnumerator;
        private readonly ProcessDistribution procs;
        private readonly FetiDPSubdomainFlexibilityMatrix subdomainFlexibility;

        public FetiDPFlexibilityMatrixMpi(ProcessDistribution procs, IModel model, IFetiDPDofSeparator dofSeparator, 
            ILagrangeMultipliersEnumerator lagrangesEnumerator, IFetiDPMatrixManager matrixManager) 
        {
            this.procs = procs;
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
            BroadcastVector(ref vIn, lagrangesEnumerator.NumLagrangeMultipliers);
            Vector subdomainRhs = subdomainFlexibility.MultiplySubdomainFIrc(vIn);
            return SumVector(subdomainRhs);
        }

        public Vector MultiplyGlobalFIrcTransposed(Vector vIn)
        {
            if (procs.IsMasterProcess)
            {
                FetiDPFlexibilityMatrixUtilities.CheckMultiplicationGlobalFIrcTransposed(vIn, dofSeparator, lagrangesEnumerator);
            }
            BroadcastVector(ref vIn, lagrangesEnumerator.NumLagrangeMultipliers);
            Vector subdomainRhs = subdomainFlexibility.MultiplySubdomainFIrcTransposed(vIn);
            return SumVector(subdomainRhs);
        }

        public void MultiplyGlobalFIrr(Vector vIn, Vector vOut)
        {
            if (procs.IsMasterProcess)
            {
                FetiDPFlexibilityMatrixUtilities.CheckMultiplicationGlobalFIrr(vIn, vOut, lagrangesEnumerator);
            }
            BroadcastVector(ref vIn, lagrangesEnumerator.NumLagrangeMultipliers);
            Vector subdomainRhs = subdomainFlexibility.MultiplySubdomainFIrr(vIn);
            SumVector(subdomainRhs, vOut);
        }

        private void BroadcastVector(ref Vector vector, int length)
        {
            //TODO: Use a dedicated class for MPI communication of Vector. This class belongs to a project LinearAlgebra.MPI.
            //      Avoid copying the array.
            double[] asArray = null;
            if (procs.IsMasterProcess) asArray = vector.CopyToArray();
            else asArray = new double[length];
            procs.Communicator.Broadcast<double>(ref asArray, procs.MasterProcess);
            vector = Vector.CreateFromArray(asArray);
        }

        private Vector SumVector(Vector vector)
        {
            double[] asArray = vector.CopyToArray();
            double[] sum = procs.Communicator.Reduce<double>(asArray, Operation<double>.Add, procs.MasterProcess);
            if (procs.IsMasterProcess) return Vector.CreateFromArray(sum);
            else return null;
        }

        private void SumVector(Vector subdomainVector, Vector globalVector)
        {
            double[] asArray = subdomainVector.CopyToArray();
            double[] sum = procs.Communicator.Reduce<double>(asArray, Operation<double>.Add, procs.MasterProcess);
            if (procs.IsMasterProcess) globalVector.CopyFrom(Vector.CreateFromArray(sum));
        }
    }
}
