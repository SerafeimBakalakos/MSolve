using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.Augmentation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.FlexibilityMatrix
{
    public class FetiDP3dFlexibilityMatrixSerial : IFetiDPFlexibilityMatrix
    {
        private readonly IAugmentationConstraints augmentationConstraints;
        private readonly IFetiDPDofSeparator dofSeparator;
        private readonly ILagrangeMultipliersEnumerator lagrangesEnumerator;
        private readonly Dictionary<ISubdomain, FetiDPSubdomainFlexibilityMatrix> subdomainFlexibilities;

        public FetiDP3dFlexibilityMatrixSerial(IModel model, IFetiDPDofSeparator dofSeparator, 
            ILagrangeMultipliersEnumerator lagrangesEnumerator, IAugmentationConstraints augmentationConstraints, 
            IFetiDPMatrixManager matrixManager) 
        {
            this.dofSeparator = dofSeparator;
            this.augmentationConstraints = augmentationConstraints;
            this.lagrangesEnumerator = lagrangesEnumerator;
            this.NumGlobalLagrangeMultipliers = lagrangesEnumerator.NumLagrangeMultipliers;

            this.subdomainFlexibilities = new Dictionary<ISubdomain, FetiDPSubdomainFlexibilityMatrix>();
            foreach (ISubdomain sub in model.EnumerateSubdomains())
            {
                this.subdomainFlexibilities[sub] = new FetiDPSubdomainFlexibilityMatrix(sub, dofSeparator, lagrangesEnumerator, 
                    matrixManager);
            }
        }

        public int NumGlobalLagrangeMultipliers { get; }

        public Vector MultiplyGlobalFIrc(Vector vIn)
        {
            FetiDPFlexibilityMatrixUtilities.CheckMultiplicationGlobalFIrc(vIn, dofSeparator, lagrangesEnumerator);
            var vOut = Vector.CreateZero(lagrangesEnumerator.NumLagrangeMultipliers);
            foreach (ISubdomain sub in subdomainFlexibilities.Keys)
            {
                Vector subdomainRhs = subdomainFlexibilities[sub].MultiplySubdomainFIrc(vIn);
                vOut.AddIntoThis(subdomainRhs);
            }
            return vOut;
        }

        public Vector MultiplyGlobalFIrcTransposed(Vector vIn)
        {
            FetiDPFlexibilityMatrixUtilities.CheckMultiplicationGlobalFIrcTransposed(vIn, dofSeparator, lagrangesEnumerator);
            var vOut = Vector.CreateZero(dofSeparator.NumGlobalCornerDofs + augmentationConstraints.NumGlobalAugmentationConstraints);
            foreach (ISubdomain sub in subdomainFlexibilities.Keys)
            {
                Vector subdomainRhs = subdomainFlexibilities[sub].MultiplySubdomainFIrcTransposed(vIn);
                vOut.AddIntoThis(subdomainRhs);
            }
            return vOut;
        }

        public void MultiplyGlobalFIrr(Vector vIn, Vector vOut)
        {
            FetiDPFlexibilityMatrixUtilities.CheckMultiplicationGlobalFIrr(vIn, vOut, lagrangesEnumerator);
            vOut.Clear();
            foreach (ISubdomain sub in subdomainFlexibilities.Keys)
            {
                Vector subdomainRhs = subdomainFlexibilities[sub].MultiplySubdomainFIrr(vIn);
                vOut.AddIntoThis(subdomainRhs);
            }
        }
    }
}
