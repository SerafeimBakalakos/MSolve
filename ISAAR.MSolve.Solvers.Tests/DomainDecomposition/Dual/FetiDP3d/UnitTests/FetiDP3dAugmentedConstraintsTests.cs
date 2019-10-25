using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.Augmentation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP3d.Example4x4x4Quads;
using Xunit;

namespace ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP3d.UnitTests
{
    public static class FetiDP3dAugmentedConstraintsTests
    {
        private enum ConstraintsSelection
        {
            Farhat, Gerasimos 
        }

        [Theory]
        [InlineData(ConstraintsSelection.Farhat)]
        [InlineData(ConstraintsSelection.Gerasimos)]
        private static void TestMatrixQr(ConstraintsSelection constraints)
        {
            (IModel model, FetiDPDofSeparatorSerial dofSeparator, LagrangeMultipliersEnumeratorSerial lagrangesEnumerator) =
                FetiDP3dLagrangesEnumeratorSerialTests.CreateModelDofSeparatorLagrangesEnumerator();

            Dictionary<ISubdomain, HashSet<INode>> midsideNodes = ModelCreator.DefineMidsideNodesAll(model);
            IMidsideNodesSelection midsideNodesSelection = new UsedDefinedMidsideNodes(midsideNodes);

            IAugmentationConstraints augmentationConstraints;
            int numAugmentationConstraints; 
            Matrix QrExpected;
            if (constraints == ConstraintsSelection.Farhat)
            {
                IDofType[] dofsPerNode = { StructuralDof.TranslationX, StructuralDof.TranslationY, StructuralDof.TranslationZ };
                augmentationConstraints =
                    new AugmentationConstraintsGlobal(midsideNodesSelection, dofsPerNode, lagrangesEnumerator);
                numAugmentationConstraints = ExpectedConnectivityData.NumGlobalAugmentationConstraintsCase2;
                QrExpected = ExpectedConnectivityData.MatrixQrCase2;
            }
            else
            {
                augmentationConstraints = 
                    new AugmentationConstraintsGlobalGerasimos(midsideNodesSelection, lagrangesEnumerator);
                numAugmentationConstraints = ExpectedConnectivityData.NumGlobalAugmentationConstraintsCase1;
                QrExpected = ExpectedConnectivityData.MatrixQrCase1;
            }

            string pathEx = @"C:\Users\Serafeim\Desktop\MPI\Tests\expectedQr.txt";
            string path = @"C:\Users\Serafeim\Desktop\MPI\Tests\Qr.txt";
            var writer = new LinearAlgebra.Output.FullMatrixWriter();
            writer.WriteToFile(QrExpected, pathEx);
            writer.WriteToFile(augmentationConstraints.MatrixQr, path);

            // Check
            Assert.Equal(numAugmentationConstraints, augmentationConstraints.NumGlobalAugmentationConstraints);
            double tolerance = 1E-13;
            Assert.True(QrExpected.Equals(augmentationConstraints.MatrixQr, tolerance));
        }
    }
}
