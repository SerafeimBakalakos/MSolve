using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Commons;
using ISAAR.MSolve.LinearAlgebra.Vectors;

//TODO: Needs another version for boolean matrices, where the values array is omitted
//TODO: What happens if a column has only zero entries? Verify that the default 0 in rowIndices and values arrays good enough. 
//      Also add respective tests.
namespace ISAAR.MSolve.LinearAlgebra.Matrices.Operators
{
    public class LocalToGlobalMappingMatrix : IMappingMatrix
    {
        private readonly int[] rowIndices;
        private readonly double[] values;

        public LocalToGlobalMappingMatrix(int numRows, double[] values, int[] rowIndices)
        {
            this.rowIndices = rowIndices;
            this.values = values;
            this.NumRows = numRows;
            this.NumColumns = rowIndices.Length;
        }

        public int NumColumns { get; }

        public int NumRows { get; }

        public Vector Multiply(Vector vector, bool transposeThis = false)
        {
            if (transposeThis) return MultiplyVectorTransposed(vector);
            else return MultiplyVectorUntransposed(vector);
        }

        public Matrix MultiplyRight(Matrix other, bool transposeThis = false)
        {
            if (transposeThis) return MultiplyRightTransposed(other);
            else return MultiplyRightUntransposed(other);
        }

        private Matrix MultiplyRightTransposed(Matrix other)
        {
            Preconditions.CheckMultiplicationDimensions(this.NumRows, other.NumRows);
            int numRowsResult = this.NumColumns;
            int numColsResult = other.NumColumns;
            var result = new double[numRowsResult * numColsResult];
            for (int col = 0; col < numColsResult; ++col)
            {
                int offset = col * numRowsResult;
                for (int j = 0; j < this.NumColumns; ++j)
                {
                    result[offset + j] = values[j] * other[rowIndices[j], col];
                }
            }
            return Matrix.CreateFromArray(result, numRowsResult, numColsResult, false);
        }

        private Matrix MultiplyRightUntransposed(Matrix other)
        {
            Preconditions.CheckMultiplicationDimensions(this.NumColumns, other.NumRows);
            int numRowsResult = this.NumRows;
            int numColsResult = other.NumColumns;
            var result = new double[numRowsResult * numColsResult];
            for (int col = 0; col < numColsResult; ++col)
            {
                int offset = col * numRowsResult;
                for (int j = 0; j < this.NumColumns; ++j)
                {
                    result[offset + rowIndices[j]] = values[j] * other[j, col];
                }
            }
            return Matrix.CreateFromArray(result, numRowsResult, numColsResult, false);
        }

        private Vector MultiplyVectorTransposed(Vector vector)
        {
            Preconditions.CheckMultiplicationDimensions(NumRows, vector.Length);
            var result = new double[NumColumns];
            for (int j = 0; j < NumColumns; ++j)
            {
                result[j] = values[j] * vector[rowIndices[j]];
            }
            return Vector.CreateFromArray(result, false);
        }

        private Vector MultiplyVectorUntransposed(Vector vector)
        {
            Preconditions.CheckMultiplicationDimensions(NumColumns, vector.Length);
            var result = new double[NumRows];
            for (int j = 0; j < NumColumns; ++j)
            {
                result[rowIndices[j]] = values[j] * vector[j];
            }
            return Vector.CreateFromArray(result, false);
        }
    }
}
