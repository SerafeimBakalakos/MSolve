using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Reordering;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.Augmentation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.StiffnessMatrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.LinearSystems;
using ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP3d.Example4x4x4Quads;
using ISAAR.MSolve.Solvers.Tests.Utilities;
using Xunit;

//TODO: Mock all other FETI classes.
//TODO: Also check diagonal Kii. Actually there are a lot of missing stuff to check.
namespace ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP3d.UnitTests
{ 
    public static class FetiDP3dMatrixManagerSerialTests
    {
        public enum MatrixFormat
        {
            Dense, Skyline
        }

        [Theory]
        [InlineData(MatrixFormat.Dense)]
        //[InlineData(MatrixFormat.Skyline)]
        public static void TestCoarseProblemMatrixAndRhs(MatrixFormat format)
        {
            IFetiDP3dMatrixManagerFactory matricesFactory = DefineMatrixManagerFactory(format);
            (IModel model, FetiDPDofSeparatorSerial dofSeparator, LagrangeMultipliersEnumeratorSerial lagrangesEnumerator) =
                FetiDP3dLagrangesEnumeratorSerialTests.CreateModelDofSeparatorLagrangesEnumerator();
            IAugmentationConstraints augmentationConstraints =
                FetiDP3dAugmentedConstraintsTests.CalcAugmentationConstraintsSimple(model, lagrangesEnumerator);
            FetiDP3dMatrixManagerSerial matrixManager = PrepareCoarseProblemSubdomainMatrices(model, dofSeparator,
                lagrangesEnumerator, augmentationConstraints, matricesFactory);

            foreach (ISubdomain sub in model.EnumerateSubdomains())
            {
                // Input data
                IFetiDPSubdomainMatrixManager subdomainMatrices = matrixManager.GetFetiDPSubdomainMatrixManager(sub);
                SetSkylineLinearSystemMatrix(subdomainMatrices.LinearSystem, ExpectedSubdomainMatrices.GetMatrixKff(sub.ID));
                subdomainMatrices.LinearSystem.RhsConcrete = ExpectedSubdomainMatrices.GetVectorFf(sub.ID);

                // Prepare the subdomain data
                subdomainMatrices.ExtractCornerRemainderSubmatrices();
                subdomainMatrices.ExtractCornerRemainderRhsSubvectors();
                subdomainMatrices.InvertKrr(true);
            }

            // Calculate the global data to test
            matrixManager.CalcInverseCoarseProblemMatrix(ModelCreator.DefineCornerNodeSelectionSerial(model));
            matrixManager.CalcCoarseProblemRhs();

            // Create explicit matrices from the matrix manager
            int coarseProblemSize = dofSeparator.NumGlobalCornerDofs + augmentationConstraints.NumGlobalAugmentationConstraints;
            Matrix globalInverseKccStarTilde = ImplicitMatrixUtilities.MultiplyWithIdentity(coarseProblemSize,
                coarseProblemSize, matrixManager.MultiplyInverseCoarseProblemMatrix);

            // Check
            double tol = 1E-3;
            Assert.True(ExpectedGlobalMatrices.MatrixGlobalKccStarTildeSimple.Invert().Equals(globalInverseKccStarTilde, tol));
            Assert.True(ExpectedGlobalMatrices.VectorGlobalFcStar.Equals(matrixManager.CoarseProblemRhs, tol));
        }


        [Theory]
        [InlineData(MatrixFormat.Dense)]
        //[InlineData(MatrixFormat.Skyline)]
        public static void TestMatricesKbbKbiKii(MatrixFormat format)
        {
            IFetiDP3dMatrixManagerFactory matricesFactory = DefineMatrixManagerFactory(format);
            (IModel model, FetiDPDofSeparatorSerial dofSeparator, LagrangeMultipliersEnumeratorSerial lagrangesEnumerator) =
                FetiDP3dLagrangesEnumeratorSerialTests.CreateModelDofSeparatorLagrangesEnumerator();
            IAugmentationConstraints augmentationConstraints =
                FetiDP3dAugmentedConstraintsTests.CalcAugmentationConstraintsSimple(model, lagrangesEnumerator);
            FetiDP3dMatrixManagerSerial matrixManager = PrepareCoarseProblemSubdomainMatrices(model, dofSeparator,
                lagrangesEnumerator, augmentationConstraints, matricesFactory);

            foreach (ISubdomain sub in model.EnumerateSubdomains())
            {
                // Input data
                IFetiDPSubdomainMatrixManager subdomainMatrices = matrixManager.GetFetiDPSubdomainMatrixManager(sub);
                SetSkylineLinearSystemMatrix(subdomainMatrices.LinearSystem, ExpectedSubdomainMatrices.GetMatrixKff(sub.ID));

                // Calculate the matrices to test
                subdomainMatrices.ExtractCornerRemainderSubmatrices();
                subdomainMatrices.ExtractKbb();
                subdomainMatrices.ExtractKbiKib();
                subdomainMatrices.CalcInverseKii(false);

                // Create explicit matrices from the matrix manager
                int numBoundaryDofs = dofSeparator.GetBoundaryDofIndices(sub).Length;
                int numInternalDofs = dofSeparator.GetInternalDofIndices(sub).Length;
                Matrix Kbb = ImplicitMatrixUtilities.MultiplyWithIdentity(
                    numBoundaryDofs, numBoundaryDofs, subdomainMatrices.MultiplyKbbTimes);
                Matrix Kbi = ImplicitMatrixUtilities.MultiplyWithIdentity(
                    numBoundaryDofs, numInternalDofs, subdomainMatrices.MultiplyKbiTimes);
                Matrix inverseKii = ImplicitMatrixUtilities.MultiplyWithIdentity(
                    numInternalDofs, numInternalDofs, x => subdomainMatrices.MultiplyInverseKiiTimes(x, false));

                // Check
                double tol = 1E-13;
                Assert.True(ExpectedSubdomainMatrices.GetMatrixKbb(sub.ID).Equals(Kbb, tol));
                Assert.True(ExpectedSubdomainMatrices.GetMatrixKbi(sub.ID).Equals(Kbi, tol));
                Assert.True(ExpectedSubdomainMatrices.GetMatrixKii(sub.ID).Invert().Equals(inverseKii, tol));
            }
        }

        [Theory]
        [InlineData(MatrixFormat.Dense)]
        //[InlineData(MatrixFormat.Skyline)]
        public static void TestMatricesKcrKrr(MatrixFormat format)
        {
            IFetiDP3dMatrixManagerFactory matricesFactory = DefineMatrixManagerFactory(format);
            (IModel model, FetiDPDofSeparatorSerial dofSeparator, LagrangeMultipliersEnumeratorSerial lagrangesEnumerator) =
                FetiDP3dLagrangesEnumeratorSerialTests.CreateModelDofSeparatorLagrangesEnumerator();
            IAugmentationConstraints augmentationConstraints =
                FetiDP3dAugmentedConstraintsTests.CalcAugmentationConstraintsSimple(model, lagrangesEnumerator);
            FetiDP3dMatrixManagerSerial matrixManager = PrepareCoarseProblemSubdomainMatrices(model, dofSeparator,
                lagrangesEnumerator, augmentationConstraints, matricesFactory);

            foreach (ISubdomain sub in model.EnumerateSubdomains())
            {
                // Input data
                IFetiDPSubdomainMatrixManager subdomainMatrices = matrixManager.GetFetiDPSubdomainMatrixManager(sub);
                SetSkylineLinearSystemMatrix(subdomainMatrices.LinearSystem, ExpectedSubdomainMatrices.GetMatrixKff(sub.ID));

                // Calculate the matrices to test
                subdomainMatrices.ExtractCornerRemainderSubmatrices();
                subdomainMatrices.InvertKrr(true);

                // Create explicit matrices from the matrix manager
                int numCornerDofs = dofSeparator.GetCornerDofIndices(sub).Length;
                int numRemainderDofs = dofSeparator.GetRemainderDofIndices(sub).Length;
                Matrix Kcc = ImplicitMatrixUtilities.MultiplyWithIdentity(
                    numCornerDofs, numCornerDofs, subdomainMatrices.MultiplyKccTimes);
                Matrix Krc = ImplicitMatrixUtilities.MultiplyWithIdentity(
                    numRemainderDofs, numCornerDofs, subdomainMatrices.MultiplyKrcTimes);
                Matrix inverseKrr = ImplicitMatrixUtilities.MultiplyWithIdentity(
                    numRemainderDofs, numRemainderDofs, subdomainMatrices.MultiplyInverseKrrTimes);

                // Check
                double tol = 1E-13;
                Assert.True(ExpectedSubdomainMatrices.GetMatrixKcc(sub.ID).Equals(Kcc, tol));
                Assert.True(ExpectedSubdomainMatrices.GetMatrixKrc(sub.ID).Equals(Krc, tol));
                Assert.True(ExpectedSubdomainMatrices.GetMatrixKrr(sub.ID).Invert().Equals(inverseKrr, tol));
            }
        }

        [Theory]
        [InlineData(MatrixFormat.Dense)]
        //[InlineData(MatrixFormat.Skyline)]
        public static void TestStaticCondensations(MatrixFormat format)
        {
            IFetiDP3dMatrixManagerFactory matricesFactory = DefineMatrixManagerFactory(format);
            (IModel model, FetiDPDofSeparatorSerial dofSeparator, LagrangeMultipliersEnumeratorSerial lagrangesEnumerator) =
                FetiDP3dLagrangesEnumeratorSerialTests.CreateModelDofSeparatorLagrangesEnumerator();
            IAugmentationConstraints augmentationConstraints =
                FetiDP3dAugmentedConstraintsTests.CalcAugmentationConstraintsSimple(model, lagrangesEnumerator);
            FetiDP3dMatrixManagerSerial matrixManager = PrepareCoarseProblemSubdomainMatrices(model, dofSeparator,
                lagrangesEnumerator, augmentationConstraints, matricesFactory);

            foreach (ISubdomain sub in model.EnumerateSubdomains())
            {
                // Input data
                IFetiDPSubdomainMatrixManager subdomainMatrices = matrixManager.GetFetiDPSubdomainMatrixManager(sub);
                SetSkylineLinearSystemMatrix(subdomainMatrices.LinearSystem, ExpectedSubdomainMatrices.GetMatrixKff(sub.ID));
                subdomainMatrices.LinearSystem.RhsConcrete = ExpectedSubdomainMatrices.GetVectorFf(sub.ID);

                // Calculate the data to test
                subdomainMatrices.ExtractCornerRemainderSubmatrices();
                subdomainMatrices.ExtractCornerRemainderRhsSubvectors();
                subdomainMatrices.InvertKrr(true);
                subdomainMatrices.CondenseMatricesStatically();
                subdomainMatrices.CondenseRhsVectorsStatically();

                // Check
                double tol = 1E-5;
                Assert.True(ExpectedSubdomainMatrices.GetMatrixKccStar(sub.ID).Equals(subdomainMatrices.KccStar, tol));
                Assert.True(ExpectedSubdomainMatrices.GetVectorFcStar(sub.ID).Equals(subdomainMatrices.FcStar, tol));
            }
        }

        [Fact]
        public static void TestVectorsFbcFr()
        {
            IFetiDP3dMatrixManagerFactory matricesFactory = new FetiDP3dMatrixManagerFactoryDense();
            (IModel model, FetiDPDofSeparatorSerial dofSeparator, LagrangeMultipliersEnumeratorSerial lagrangesEnumerator) =
                FetiDP3dLagrangesEnumeratorSerialTests.CreateModelDofSeparatorLagrangesEnumerator();
            IAugmentationConstraints augmentationConstraints =
                FetiDP3dAugmentedConstraintsTests.CalcAugmentationConstraintsSimple(model, lagrangesEnumerator);
            FetiDP3dMatrixManagerSerial matrixManager = new FetiDP3dMatrixManagerSerial(model, dofSeparator, lagrangesEnumerator,
                augmentationConstraints, matricesFactory);

            foreach (ISubdomain sub in model.EnumerateSubdomains())
            {
                // Calculate the necessary vectors
                IFetiDPSubdomainMatrixManager subdomainMatrices = matrixManager.GetFetiDPSubdomainMatrixManager(sub);
                subdomainMatrices.LinearSystem.RhsConcrete = ExpectedSubdomainMatrices.GetVectorFf(sub.ID);
                subdomainMatrices.ExtractCornerRemainderRhsSubvectors();

                // Check
                double tol = 1E-13;
                Assert.True(ExpectedSubdomainMatrices.GetVectorFbc(sub.ID).Equals(subdomainMatrices.Fbc, tol));
                Assert.True(ExpectedSubdomainMatrices.GetVectorFr(sub.ID).Equals(subdomainMatrices.Fr, tol));
            }
        }

        internal static IFetiDP3dMatrixManagerFactory DefineMatrixManagerFactory(MatrixFormat format)
        {
            if (format == MatrixFormat.Dense) return new FetiDP3dMatrixManagerFactoryDense();
            else if (format == MatrixFormat.Skyline) throw new NotImplementedException();
            else throw new NotImplementedException();
        }

        internal static FetiDP3dMatrixManagerSerial PrepareCoarseProblemSubdomainMatrices(IModel model,
            IFetiDPDofSeparator dofSeparator, ILagrangeMultipliersEnumerator lagrangesEnumerator, 
            IAugmentationConstraints augmentationConstraints, IFetiDP3dMatrixManagerFactory matricesFactory)
        {
            var matrixManager = new FetiDP3dMatrixManagerSerial(model, dofSeparator, lagrangesEnumerator, 
                augmentationConstraints, matricesFactory);
            foreach (ISubdomain sub in model.EnumerateSubdomains())
            {
                // Input data
                IFetiDPSubdomainMatrixManager subdomainMatrices = matrixManager.GetFetiDPSubdomainMatrixManager(sub);
                SetSkylineLinearSystemMatrix(subdomainMatrices.LinearSystem, ExpectedSubdomainMatrices.GetMatrixKff(sub.ID));
                subdomainMatrices.LinearSystem.RhsConcrete = ExpectedSubdomainMatrices.GetVectorFf(sub.ID);

                // Prepare the subdomain data
                subdomainMatrices.ExtractCornerRemainderSubmatrices();
                subdomainMatrices.ExtractCornerRemainderRhsSubvectors();
                subdomainMatrices.InvertKrr(true);
            }

            return matrixManager;
        }

        //TODO: Jesus Christ! It is way too difficult to mess with linear system classes, even using reflection.
        //      I should allow some provisions for testing. Also ILinearSystem.Matrix {set;} should be internal. Analyzers and 
        //      providers should not even try.
        internal static void SetSkylineLinearSystemMatrix(ISingleSubdomainLinearSystem linearSystem, IMatrixView matrix)
        {
            var castedLS = (LinearSystemBase<SkylineMatrix, Vector>)linearSystem;
            castedLS.Matrix = SkylineMatrix.CreateFromMatrix(matrix);
        }
    }
}
