﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Commons;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.Augmentation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;

//TODO: Useless checks probably. Should be removed
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.FlexibilityMatrix
{
    public static class FetiDPFlexibilityMatrixUtilities
    {
        [Conditional("DEBUG")]
        public static void CheckMultiplicationGlobalFIrc(Vector vIn, IFetiDPDofSeparator dofSeparator, 
            ILagrangeMultipliersEnumerator lagrangeEnumerator)
        {
            Preconditions.CheckMultiplicationDimensions(dofSeparator.NumGlobalCornerDofs, vIn.Length);
        }

        [Conditional("DEBUG")]
        public static void CheckMultiplicationGlobalFIrc3d(Vector vIn, IFetiDPDofSeparator dofSeparator,
            ILagrangeMultipliersEnumerator lagrangeEnumerator, IAugmentationConstraints augmentationConstraints)
        {
            Preconditions.CheckMultiplicationDimensions(
                dofSeparator.NumGlobalCornerDofs + augmentationConstraints.NumGlobalAugmentationConstraints, vIn.Length);
        }

        [Conditional("DEBUG")]
        public static void CheckMultiplicationGlobalFIrcTransposed(Vector vIn, IFetiDPDofSeparator dofSeparator,
            ILagrangeMultipliersEnumerator lagrangeEnumerator)
        {
            Preconditions.CheckMultiplicationDimensions(lagrangeEnumerator.NumLagrangeMultipliers, vIn.Length);
        }

        [Conditional("DEBUG")]
        public static void CheckMultiplicationGlobalFIrr(Vector vIn, Vector vOut, 
            ILagrangeMultipliersEnumerator lagrangeEnumerator)
        {
            Preconditions.CheckMultiplicationDimensions(lagrangeEnumerator.NumLagrangeMultipliers, vIn.Length);
            Preconditions.CheckSystemSolutionDimensions(lagrangeEnumerator.NumLagrangeMultipliers, vOut.Length);
        }
    }
}
