using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Iterative;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.LinearAlgebraExtensions
{
    public class ExpandedBlockDiagonalMatrix : IVectorMultipliable
    {
        public SortedDictionary<int, Matrix> SubdomainMatrices = new SortedDictionary<int, Matrix>();

        public int NumColumns
        {
            get
            {
                int numColsTotal = 0;
                foreach (int s in SubdomainMatrices.Keys)
                {
                    numColsTotal += SubdomainMatrices[s].NumColumns;
                }
                return numColsTotal;
            }
        }

        public int NumRows
        {
            get
            {
                int numRowsTotal = 0;
                foreach (int s in SubdomainMatrices.Keys)
                {
                    numRowsTotal += SubdomainMatrices[s].NumRows;
                }
                return numRowsTotal;
            }
        }

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

        public Matrix CopyToFullMatrix()
        {
            var result = Matrix.CreateZero(NumRows, NumColumns);
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

        public void Multiply(IVectorView lhsVector, IVector rhsVector)
        {
            var input = (ExpandedVector)lhsVector;
            var output = (ExpandedVector)rhsVector;
            output.Clear();
            foreach (int s in this.SubdomainMatrices.Keys)
            {
                output.SubdomainVectors[s] = this.SubdomainMatrices[s] * input.SubdomainVectors[s];
            }
        }

        public IVector Multiply(IVectorView lhsVector) => this * (ExpandedVector)lhsVector;


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
