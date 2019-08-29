using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Triangulation;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace ISAAR.MSolve.LinearAlgebra.SchurComplements
{
    public static class SchurComplementCsc
    {
        /// <summary>
        /// Calculates the Schur complement of A/A22 = S = A11 - A21^T * inv(A22) * A21, where M = [A11 A21; A21^T A22].
        /// This method constructs inv(A22) * A21 one column at a time and uses that column to calculate the superdiagonal
        /// entries of the corresponding column of A21^T * inv(A22) * A21.
        /// </summary>
        public static SymmetricMatrix CalcSchurComplementSymmetric(SymmetricMatrix A11, CscMatrix A21, ITriangulation inverseA22)
        { //TODO: Unfortunately this cannot take advantage of MKL for CSC^T * vector.
            double[] valuesA21 = A21.RawValues;
            int[] rowIndicesA21 = A21.RawRowIndices;
            int[] colOffsetsA21 = A21.RawColOffsets;
            var S = SymmetricMatrix.CreateZero(A11.Order);

            for (int j = 0; j < A21.NumColumns; ++j)
            {
                // column j of (inv(A22) * A21) = inv(A22) * column j of A21
                Vector colA21 = A21.GetColumn(j);
                double[] colInvCA21 = inverseA22.SolveLinearSystem(colA21).RawData;

                // column j of (A21^T * inv(A22) * A21) = A21^T * column j of (inv(A22) * A21)
                // However we only need the superdiagonal part of this column. 
                // Thus we only multiply the rows i of A21^T (stored as columns i of A21) with i <= j. 
                for (int i = 0; i <= j; ++i)
                {
                    double dot = 0.0;
                    int colStart = colOffsetsA21[i]; //inclusive
                    int colEnd = colOffsetsA21[i + 1]; //exclusive
                    for (int k = colStart; k < colEnd; ++k) dot += valuesA21[k] * colInvCA21[rowIndicesA21[k]];

                    // Perform the subtraction S = A11 - (A21^T * inv(A22) * A21) for the current (i, j)
                    int indexS = S.Find1DIndex(i, j);
                    S.RawData[indexS] = A11.RawData[indexS] - dot;
                }
            }

            return S;
        }

        public static Matrix CalcSchurComplementFull(Matrix A11, CscMatrix A21, LdlSkyline inverseA22)
        {
            // S = A11 - A21^T * inv(A22) * A21
            Matrix invCA21 = Matrix.CreateZero(inverseA22.Order, A21.NumColumns);
            inverseA22.SolveLinearSystems(A21, invCA21);
            return A11 - A21.MultiplyRight(invCA21, true);
        }
    }
}
