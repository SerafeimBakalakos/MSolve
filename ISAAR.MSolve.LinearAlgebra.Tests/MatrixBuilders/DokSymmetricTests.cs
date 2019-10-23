using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Builders;
using ISAAR.MSolve.LinearAlgebra.Tests.TestData;
using ISAAR.MSolve.LinearAlgebra.Tests.Utilities;
using Xunit;

namespace ISAAR.MSolve.LinearAlgebra.Tests.MatrixBuilders
{
    /// <summary>
    /// Tests for <see cref="DokSymmetric"/>.
    /// Authors: Serafeim Bakalakos
    /// </summary>
    public static class DokSymmetricTests
    {
        private static readonly MatrixComparer comparer = new MatrixComparer(1E-13);

        private static DokSymmetric CreateDok(double[,] symmMatrix)
        {
            int n = symmMatrix.GetLength(0);
            var dok = DokSymmetric.CreateEmpty(n);
            for (int j = 0; j < n; ++j)
            {
                for (int i = 0; i <= j; ++i)
                {
                    if (symmMatrix[i, j] != 0.0) dok[i, j] = symmMatrix[i, j];
                }
            }
            return dok;
        }

        [Fact]
        private static void TestAddSubmatrix()
        {
            var k1 = Matrix.CreateFromArray(GlobalMatrixAssembly.SubMatrix1);
            var k2 = Matrix.CreateFromArray(GlobalMatrixAssembly.SubMatrix2);
            var k3 = Matrix.CreateFromArray(GlobalMatrixAssembly.SubMatrix3);
            var expectedK = Matrix.CreateFromArray(GlobalMatrixAssembly.GlobalMatrix);

            var computedK = DokSymmetric.CreateEmpty(GlobalMatrixAssembly.GlobalOrder);
            computedK.AddSubmatrixSymmetric(k1, GlobalMatrixAssembly.IndicesDictionary1);
            computedK.AddSubmatrixSymmetric(k2, GlobalMatrixAssembly.IndicesDictionary2);
            computedK.AddSubmatrixSymmetric(k3, GlobalMatrixAssembly.IndicesDictionary3);

            comparer.AssertEqual(expectedK, computedK);
        }

        [Fact]
        private static void TestGetColumn()
        {
            Matrix dense = Matrix.CreateFromArray(SparsePosDef10by10.Matrix);
            DokSymmetric dok = CreateDok(SparsePosDef10by10.Matrix);

            for (int j = 0; j < SparsePosDef10by10.Order; ++j)
            {
                comparer.AssertEqual(dense.GetColumn(j), dok.GetColumn(j)); //TODO: have hardcoded columns to compare against
            }
        }

        [Fact]
        private static void TestGetSubmatrixDokColMajor()
        {
            // These are useful for debugging
            //string outputPath = @"C:\Users\Serafeim\Desktop\output.txt";
            //var writer = new LinearAlgebra.Output.FullMatrixWriter();

            var array2D = MultiDiagonalMatrices.CreateSymmetric(100, new int[] { 2, 4, 8, 16, 32, 64 });
            var matrixFull = Matrix.CreateFromArray(array2D);
            var matrixDok = DokSymmetric.CreateFromArray2D(array2D);

            var indices = new int[] { 0, 2, 4, 6, 12, 24, 32, 50, 64, 80 };
            var indicesPerm = new int[] { 32, 80, 64, 0, 12, 24, 6, 50, 4, 2 };
            int[] rowIndices = indicesPerm;
            var colIndices = new int[] { 90, 10, 20, 60, 40, 50, 0, 70, 80, 30 };

            DokColMajor subMatrixCsc = matrixDok.GetSubmatrixDokColMajor(indices, indices);
            Matrix subMatrixExpected = matrixFull.GetSubmatrix(indices, indices);
            Assert.True(subMatrixExpected.Equals(subMatrixCsc));

            DokColMajor subMatrixPermCsc = matrixDok.GetSubmatrixDokColMajor(indicesPerm, indicesPerm);
            Matrix subMatrixPermExpected = matrixFull.GetSubmatrix(indicesPerm, indicesPerm);
            Assert.True(subMatrixPermExpected.Equals(subMatrixPermCsc));

            DokColMajor subMatrixRectCsc = matrixDok.GetSubmatrixDokColMajor(rowIndices, colIndices);
            Matrix subMatrixRectExpected = matrixFull.GetSubmatrix(rowIndices, colIndices);
            Assert.True(subMatrixRectExpected.Equals(subMatrixRectCsc));
        }

        [Fact]
        private static void TestGetSubmatrixFull()
        {
            // These are useful for debugging
            //string outputPath = @"C:\Users\Serafeim\Desktop\output.txt";
            //var writer = new LinearAlgebra.Output.FullMatrixWriter();

            var array2D = MultiDiagonalMatrices.CreateSymmetric(100, new int[] { 2, 4, 8, 16, 32, 64 });
            var matrixFull = Matrix.CreateFromArray(array2D);
            var matrixDok = DokSymmetric.CreateFromArray2D(array2D);

            var indices = new int[] { 0, 2, 4, 6, 12, 24, 32, 50, 64, 80 };
            var indicesPerm = new int[] { 32, 80, 64, 0, 12, 24, 6, 50, 4, 2 };
            int[] rowIndices = indicesPerm;
            var colIndices = new int[] { 90, 10, 20, 60, 40, 50, 0, 70, 80, 30 };

            Matrix subMatrixFull = matrixDok.GetSubmatrixFull(indices, indices);
            Matrix subMatrixExpected = matrixFull.GetSubmatrix(indices, indices);
            Assert.True(subMatrixExpected.Equals(subMatrixFull));

            Matrix subMatrixPermFull = matrixDok.GetSubmatrixFull(indicesPerm, indicesPerm);
            Matrix subMatrixPermExpected = matrixFull.GetSubmatrix(indicesPerm, indicesPerm);
            Assert.True(subMatrixPermExpected.Equals(subMatrixPermFull));

            Matrix subMatrixRectCscFull = matrixDok.GetSubmatrixFull(rowIndices, colIndices);
            Matrix subMatrixRectExpected = matrixFull.GetSubmatrix(rowIndices, colIndices);
            Assert.True(subMatrixRectExpected.Equals(subMatrixRectCscFull));
        }

        [Fact]
        private static void TestGetSubmatrixDok()
        {
            var array2D = MultiDiagonalMatrices.CreateSymmetric(100, new int[] { 2, 4, 8, 16, 32, 64 });
            var matrixFull = Matrix.CreateFromArray(array2D);
            var matrixDok = DokSymmetric.CreateFromArray2D(array2D);

            var indices = new int[] { 0, 2, 4, 6, 12, 24, 32, 50, 64, 80 };
            var indicesPerm = new int[] { 32, 80, 64, 0, 12, 24, 6, 50, 4, 2 };

            DokSymmetric subMatrixDok = matrixDok.GetSubmatrixSymmetricDok(indices);
            //writer.WriteToFile(subMatrixSym, outputPath, true);
            Matrix subMatrixExpected = matrixFull.GetSubmatrix(indices, indices);
            Assert.True(subMatrixExpected.Equals(subMatrixDok));

            DokSymmetric subMatrixPermDok = matrixDok.GetSubmatrixSymmetricDok(indicesPerm);
            Matrix subMatrixPermExpected = matrixFull.GetSubmatrix(indicesPerm, indicesPerm);
            Assert.True(subMatrixPermExpected.Equals(subMatrixPermDok));

            DokSymmetric matrix2 = CreateDok(new double[,]
            {
                {  0,  0, 20,  0,  0,  0 },
                {  0,  1,  0, 31,  0,  0 },
                { 20,  0,  2,  0, 42,  0 },
                {  0, 31,  0,  3,  0, 53 },
                {  0,  0, 42,  0,  4,  0 },
                {  0,  0,  0, 53,  0,  5 }
            });
            var rowsToKeep2 = new int[] { 4, 2, 5 };
            DokSymmetric submatrixExpected2 = CreateDok(new double[,]
            {
                {  4, 42, 0 }, 
                { 42,  2, 0 }, 
                {  0,  0, 5 }
            });

            DokSymmetric submatrixComputed2 = matrix2.GetSubmatrixSymmetricDok(rowsToKeep2);
            comparer.AssertEqual(submatrixExpected2, submatrixComputed2);
        }

        [Fact]
        private static void TestGetSubmatrixSymmetricPacked()
        {
            //// These are useful for debugging
            //string outputPath = @"C:\Users\Serafeim\Desktop\output.txt";
            //var writer = new LinearAlgebra.Output.FullMatrixWriter();

            var array2D = MultiDiagonalMatrices.CreateSymmetric(100, new int[] { 2, 4, 8, 16, 32, 64 });
            var matrixFull = Matrix.CreateFromArray(array2D);
            var matrixDok = DokSymmetric.CreateFromArray2D(array2D);

            var indices = new int[] { 0, 2, 4, 6, 12, 24, 32, 50, 64, 80 };
            var indicesPerm = new int[] { 32, 80, 64, 0, 12, 24, 6, 50, 4, 2 };

            SymmetricMatrix subMatrixPck = matrixDok.GetSubmatrixSymmetricPacked(indices);
            //writer.WriteToFile(subMatrixSym, outputPath, true);
            Matrix subMatrixExpected = matrixFull.GetSubmatrix(indices, indices);
            Assert.True(subMatrixExpected.Equals(subMatrixPck));

            SymmetricMatrix subMatrixPermPck = matrixDok.GetSubmatrixSymmetricPacked(indicesPerm);
            Matrix subMatrixPermExpected = matrixFull.GetSubmatrix(indicesPerm, indicesPerm);
            Assert.True(subMatrixPermExpected.Equals(subMatrixPermPck));
        }

        [Fact]
        private static void TestGetSubmatrixSymmetricSkyline()
        {
            //// These are useful for debugging
            //string outputPath = @"C:\Users\Serafeim\Desktop\output.txt";
            //var writer = new LinearAlgebra.Output.FullMatrixWriter();

            var array2D = MultiDiagonalMatrices.CreateSymmetric(100, new int[] { 2, 4, 8, 16, 32, 64 });
            var matrixFull = Matrix.CreateFromArray(array2D);
            var matrixDok = DokSymmetric.CreateFromArray2D(array2D);

            var indices = new int[] { 0, 2, 4, 6, 12, 24, 32, 50, 64, 80 };
            var indicesPerm = new int[] { 32, 80, 64, 0, 12, 24, 6, 50, 4, 2 };

            SkylineMatrix subMatrixSky = matrixDok.GetSubmatrixSymmetricSkyline(indices);
            //writer.WriteToFile(subMatrixSym, outputPath, true);
            Matrix subMatrixExpected = matrixFull.GetSubmatrix(indices, indices);
            Assert.True(subMatrixExpected.Equals(subMatrixSky));

            SkylineMatrix subMatrixPermSky = matrixDok.GetSubmatrixSymmetricSkyline(indicesPerm);
            Matrix subMatrixPermExpected = matrixFull.GetSubmatrix(indicesPerm, indicesPerm);
            Assert.True(subMatrixPermExpected.Equals(subMatrixPermSky));
        }

        [Fact]
        private static void TestIndexer()
        {
            Matrix dense = Matrix.CreateFromArray(SparsePosDef10by10.Matrix);
            DokSymmetric dok = CreateDok(SparsePosDef10by10.Matrix);
            comparer.AssertEqual(dense, dok);
        }
    }
}
