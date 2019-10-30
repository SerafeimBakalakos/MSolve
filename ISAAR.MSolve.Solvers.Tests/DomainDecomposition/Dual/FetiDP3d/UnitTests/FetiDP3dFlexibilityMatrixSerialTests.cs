using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.Augmentation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.FlexibilityMatrix;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP3d.Example4x4x4Quads;
using ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP3d.UnitTests.Mocks;
using ISAAR.MSolve.Solvers.Tests.Utilities;
using Xunit;

namespace ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP3d.UnitTests
{
    public static class FetiDP3dFlexibilityMatrixSerialTests
    {
        private enum ConstraintsSelection
        {
            Simple, Redundant
        }

        [Theory]
        [InlineData(ConstraintsSelection.Simple)]
        [InlineData(ConstraintsSelection.Redundant)]
        private static void TestFlexibilityMatrices(ConstraintsSelection constraints)
        {
            (IModel model, FetiDPDofSeparatorSerial dofSeparator, LagrangeMultipliersEnumeratorSerial lagrangesEnumerator) =
                FetiDP3dLagrangesEnumeratorSerialTests.CreateModelDofSeparatorLagrangesEnumerator();

            // Augmentation
            Dictionary<ISubdomain, HashSet<INode>> midsideNodes = ModelCreator.DefineMidsideNodesAll(model);
            IMidsideNodesSelection midsideNodesSelection = new UsedDefinedMidsideNodes(midsideNodes);
            IDofType[] dofsPerNode = { StructuralDof.TranslationX, StructuralDof.TranslationY, StructuralDof.TranslationZ };

            IAugmentationConstraints augmentationConstraints;
            int numAugmentationConstraints;
            Matrix FIrcTildeExpected;

            if (constraints == ConstraintsSelection.Simple)
            {
                augmentationConstraints =
                    new AugmentationConstraints(midsideNodesSelection, dofsPerNode, lagrangesEnumerator);
                numAugmentationConstraints = ExpectedConnectivityData.NumGlobalAugmentationConstraintsCase2;
                FIrcTildeExpected = ExpectedGlobalMatrices.MatrixFIrcTilde;
            }
            else
            {
                augmentationConstraints =
                    new AugmentationConstraintsRedundant(midsideNodesSelection, dofsPerNode, lagrangesEnumerator);
                numAugmentationConstraints = ExpectedConnectivityData.NumGlobalAugmentationConstraintsCase1;
                FIrcTildeExpected = ExpectedGlobalMatrices.MatrixFIrcTildeRedundant;
            }

            // Setup matrix manager
            IFetiDPMatrixManager matrixManager = new MockMatrixManager(model);

            // Create explicit matrices that can be checked
            var flexibility = new FetiDP3dFlexibilityMatrixSerial(model, dofSeparator, lagrangesEnumerator, 
                augmentationConstraints, matrixManager);
            int numCornerDofs = dofSeparator.NumGlobalCornerDofs;
            int numLagranges = lagrangesEnumerator.NumLagrangeMultipliers;
            Matrix FIrr = ImplicitMatrixUtilities.MultiplyWithIdentity(
                numLagranges, numLagranges, flexibility.MultiplyGlobalFIrr);
            Matrix FIrcTilde = ImplicitMatrixUtilities.MultiplyWithIdentity(
                numLagranges, numCornerDofs + numAugmentationConstraints, flexibility.MultiplyGlobalFIrc);

            // Check
            double tol = 1E-3;
            Assert.True(ExpectedGlobalMatrices.MatrixFIrr.Equals(FIrr, tol));
            Assert.True(FIrcTildeExpected.Equals(FIrcTilde, tol));
        }
    }
}
