using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.LinearAlgebra.Tests.Utilities;
using Xunit;

namespace ISAAR.MSolve.LinearAlgebra.Tests.Matrices
{
    /// <summary>
    /// Tests for <see cref="UnsignedBooleanMatrix"/>.
    /// Authors: Serafeim Bakalakos
    /// </summary>
    public static class UnsignedBooleanMatrixTests
    {
        private static readonly MatrixComparer comparer = new MatrixComparer(1E-13);

        internal static UnsignedBooleanMatrix CreateBooleanMatrixOne1PerRow()
        {
            // 1 0 0 0
            // 0 1 0 0
            // 1 0 0 0
            // 0 1 0 0
            // 0 0 1 0 
            // 0 0 0 1 
            // 0 0 1 0 
            // 0 0 0 1 
            var matrix = new UnsignedBooleanMatrix(8, 4);
            matrix.AddEntry(0, 0);
            matrix.AddEntry(1, 1);
            matrix.AddEntry(2, 0);
            matrix.AddEntry(3, 1);
            matrix.AddEntry(4, 2);
            matrix.AddEntry(5, 3);
            matrix.AddEntry(6, 2);
            matrix.AddEntry(7, 3);
            return matrix;
        }

        internal static UnsignedBooleanMatrix CreateBooleanMatrixOne1PerRowAndCol()
        {
            // 1 0 0 0 0 0 0 0
            // 0 1 0 0 0 0 0 0
            // 0 0 0 0 1 0 0 0
            // 0 0 0 0 0 1 0 0
            var matrix = new UnsignedBooleanMatrix(4, 8);
            matrix.AddEntry(0, 0);
            matrix.AddEntry(1, 1);
            matrix.AddEntry(2, 4);
            matrix.AddEntry(3, 5);
            return matrix;
        }

        [Fact]
        private static void TestGetColumns()
        {
            UnsignedBooleanMatrix sparseA = CreateBooleanMatrixOne1PerRow();
            var denseA = Matrix.CreateFromMatrix(sparseA);

            int[] colsToKeepOption0 = { 1, 3 };
            int[] colsToKeepOption1 = { 2, 0, 1 };
            int[] allRows = Enumerable.Range(0, sparseA.NumRows).ToArray();

            UnsignedBooleanMatrix submatrixOption0 = sparseA.GetColumns(colsToKeepOption0);
            Matrix submatrixOption0Expected = denseA.GetSubmatrix(allRows, colsToKeepOption0);
            comparer.AssertEqual(submatrixOption0Expected, submatrixOption0);

            UnsignedBooleanMatrix submatrixOption1 = sparseA.GetColumns(colsToKeepOption1);
            Matrix submatrixOption1Expected = denseA.GetSubmatrix(allRows, colsToKeepOption1);
            comparer.AssertEqual(submatrixOption1Expected, submatrixOption1);
        }

        [Fact]
        private static void TestMultiplicationThisTransposeTimesOtherTimesThis()
        {
            int seed = 273;
            var rng = new Random(seed);

            var A = Matrix.CreateZero(4, 4);
            A.DoToAllEntriesIntoThis(Aii => rng.NextDouble());

            UnsignedBooleanMatrix B = CreateBooleanMatrixOne1PerRowAndCol();
            Matrix denseB = Matrix.CreateFromMatrix(B);

            Matrix BtAB = B.ThisTransposeTimesOtherTimesThis(A);
            Matrix expectedBtAB = denseB.ThisTransposeTimesOtherTimesThis(A);

            comparer.AssertEqual(expectedBtAB, BtAB);
        }

        [Fact]
        private static void TestMultiplyRight()
        {
            int seed = 273;
            var rng = new Random(seed);

            var A = Matrix.CreateZero(8, 4);
            A.DoToAllEntriesIntoThis(Aii => rng.NextDouble());
            Matrix C = Matrix.CreateZero(4, 9);
            C.DoToAllEntriesIntoThis(Cii => rng.NextDouble());

            UnsignedBooleanMatrix B = CreateBooleanMatrixOne1PerRowAndCol();
            Matrix denseB = Matrix.CreateFromMatrix(B);

            Matrix BA = B.MultiplyRight(A);
            Matrix expectedBA = denseB.MultiplyRight(A);
            comparer.AssertEqual(expectedBA, BA);

            Matrix BtC = B.MultiplyRight(C, true);
            Matrix expectedBtC = denseB.MultiplyRight(C, true);
            comparer.AssertEqual(expectedBtC, BtC);
        }
    }
}
