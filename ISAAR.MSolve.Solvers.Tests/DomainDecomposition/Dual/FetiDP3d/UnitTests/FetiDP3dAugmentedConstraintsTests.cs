using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.Augmentation;
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
            Simple, Redundant 
        }

        [Theory]
        [InlineData(ConstraintsSelection.Simple)]
        [InlineData(ConstraintsSelection.Redundant)]
        private static void TestMatrixQr(ConstraintsSelection constraints)
        {
            (IModel model, FetiDPDofSeparatorSerial dofSeparator, LagrangeMultipliersEnumeratorSerial lagrangesEnumerator) =
                FetiDP3dLagrangesEnumeratorSerialTests.CreateModelDofSeparatorLagrangesEnumerator();

            Dictionary<ISubdomain, HashSet<INode>> midsideNodes = ModelCreator.DefineMidsideNodesAll(model);
            IMidsideNodesSelection midsideNodesSelection = new UsedDefinedMidsideNodes(midsideNodes);
            IDofType[] dofsPerNode = { StructuralDof.TranslationX, StructuralDof.TranslationY, StructuralDof.TranslationZ };

            IAugmentationConstraints augmentationConstraints;
            int numAugmentationConstraints; 
            Matrix QrExpected;
            if (constraints == ConstraintsSelection.Simple)
            {
                augmentationConstraints =
                    new AugmentationConstraints(midsideNodesSelection, dofsPerNode, lagrangesEnumerator);
                numAugmentationConstraints = ExpectedConnectivityData.NumGlobalAugmentationConstraintsCase2;
                QrExpected = ExpectedConnectivityData.MatrixQr;
            }
            else
            {
                augmentationConstraints = 
                    new AugmentationConstraintsRedundant(midsideNodesSelection, dofsPerNode, lagrangesEnumerator);
                numAugmentationConstraints = ExpectedConnectivityData.NumGlobalAugmentationConstraintsCase1;
                QrExpected = ExpectedConnectivityData.MatrixQrRedundant;
            }

            //string pathEx = @"C:\Users\Serafeim\Desktop\MPI\Tests\expectedQr.txt";
            //string path = @"C:\Users\Serafeim\Desktop\MPI\Tests\Qr.txt";
            //string pathDiff = @"C:\Users\Serafeim\Desktop\MPI\Tests\diffQr.txt";
            //var writer = new LinearAlgebra.Output.FullMatrixWriter();
            //writer.WriteToFile(QrExpected, pathEx);
            //writer.WriteToFile(augmentationConstraints.MatrixQr, path);
            //writer.WriteToFile(QrExpected - augmentationConstraints.MatrixQr, pathDiff);

            // Check
            Assert.Equal(numAugmentationConstraints, augmentationConstraints.NumGlobalAugmentationConstraints);
            double tolerance = 1E-13;
            Assert.True(QrExpected.Equals(augmentationConstraints.MatrixQr, tolerance));
        }
    }
}
