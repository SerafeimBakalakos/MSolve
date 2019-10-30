using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.Augmentation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;

//TODO: The serial/MPI coordinators of regular FETI-DP should be used instead of this. Only the subdomain operations and 
//      the dimensions are different.
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.FlexibilityMatrix
{
    public class FetiDP3dFlexibilityMatrixSerial : IFetiDPFlexibilityMatrix
    {
        private readonly IAugmentationConstraints augmentationConstraints;
        private readonly IFetiDPDofSeparator dofSeparator;
        private readonly ILagrangeMultipliersEnumerator lagrangesEnumerator;
        private readonly Dictionary<ISubdomain, FetiDP3dSubdomainFlexibilityMatrix> subdomainFlexibilities;

        public FetiDP3dFlexibilityMatrixSerial(IModel model, IFetiDPDofSeparator dofSeparator, 
            ILagrangeMultipliersEnumerator lagrangesEnumerator, IAugmentationConstraints augmentationConstraints, 
            IFetiDPMatrixManager matrixManager) 
        {
            this.dofSeparator = dofSeparator;
            this.augmentationConstraints = augmentationConstraints;
            this.lagrangesEnumerator = lagrangesEnumerator;
            this.NumGlobalLagrangeMultipliers = lagrangesEnumerator.NumLagrangeMultipliers;

            this.subdomainFlexibilities = new Dictionary<ISubdomain, FetiDP3dSubdomainFlexibilityMatrix>();
            foreach (ISubdomain sub in model.EnumerateSubdomains())
            {
                this.subdomainFlexibilities[sub] = new FetiDP3dSubdomainFlexibilityMatrix(sub, dofSeparator, lagrangesEnumerator,
                    augmentationConstraints, matrixManager);
            }
        }

        public int NumGlobalLagrangeMultipliers { get; }

        public Vector MultiplyGlobalFIrc(Vector vIn)
        {
            FetiDPFlexibilityMatrixUtilities.CheckMultiplicationGlobalFIrc3d(vIn, dofSeparator, lagrangesEnumerator, 
                augmentationConstraints);
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
