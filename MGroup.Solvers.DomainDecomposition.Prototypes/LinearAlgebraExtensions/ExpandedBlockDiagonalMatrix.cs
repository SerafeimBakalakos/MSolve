using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.LinearAlgebraExtensions
{
    public class ExpandedBlockDiagonalMatrix
    {
        public SortedDictionary<int, Matrix> SubdomainMatrices = new SortedDictionary<int, Matrix>();

        public static ExpandedVector operator *(ExpandedBlockDiagonalMatrix matrix, ExpandedVector vector)
        {
            var result = new ExpandedVector();
            foreach (int s in matrix.SubdomainMatrices.Keys)
            {
                result.SubdomainVectors[s] = matrix.SubdomainMatrices[s] * vector.SubdomainVectors[s];
            }
            return result;
        }

        public static ExpandedBlockDiagonalMatrix operator *(ExpandedBlockDiagonalMatrix matrix1, ExpandedBlockDiagonalMatrix matrix2)
        {
            var result = new ExpandedBlockDiagonalMatrix();
            foreach (int s in matrix1.SubdomainMatrices.Keys)
            {
                result.SubdomainMatrices[s] = matrix1.SubdomainMatrices[s] * matrix2.SubdomainMatrices[s];
            }
            return result;
        }

        public Matrix ToFullMatrix()
        {
            int numRowsTotal = 0;
            int numColsTotal = 0;
            foreach (int s in SubdomainMatrices.Keys)
            {
                numRowsTotal += SubdomainMatrices[s].NumRows;
                numColsTotal += SubdomainMatrices[s].NumColumns;
            }

            var result = Matrix.CreateZero(numRowsTotal, numColsTotal);
            int rowStart = 0;
            int colStart = 0;
            foreach (int s in SubdomainMatrices.Keys)
            {
                result.SetSubmatrix(rowStart, colStart, SubdomainMatrices[s]);
                rowStart += SubdomainMatrices[s].NumRows;
                colStart += SubdomainMatrices[s].NumColumns;
            }

            return result;
        }

        public ExpandedBlockDiagonalMatrix Transpose()
        {
            var result = new ExpandedBlockDiagonalMatrix();
            foreach (int s in this.SubdomainMatrices.Keys)
            {
                result.SubdomainMatrices[s] = this.SubdomainMatrices[s].Transpose();
            }
            return result;
        }
    }
}
