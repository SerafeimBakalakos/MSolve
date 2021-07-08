using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.LinearAlgebraExtensions
{
    public class ExpandedBlockRowMatrix
    {
        public ExpandedBlockRowMatrix(int numRows)
        {
            NumRows = numRows;
        }

        public SortedDictionary<int, Matrix> SubdomainMatrices = new SortedDictionary<int, Matrix>();

        public int NumRows { get; }

        public static Vector operator *(ExpandedBlockRowMatrix matrix, ExpandedVector vector)
        {
            var result = Vector.CreateZero(matrix.NumRows);
            foreach (int s in matrix.SubdomainMatrices.Keys)
            {
                result.AddIntoThis(matrix.SubdomainMatrices[s] * vector.SubdomainVectors[s]);
            }
            return result;
        }

        public Matrix ToFullMatrix()
        {
            int numColsTotal = 0;
            foreach (int s in SubdomainMatrices.Keys)
            {
                numColsTotal += SubdomainMatrices[s].NumColumns;
            }

            var result = Matrix.CreateZero(NumRows, numColsTotal);
            int colStart = 0;
            foreach (int s in SubdomainMatrices.Keys)
            {
                result.SetSubmatrix(0, colStart, SubdomainMatrices[s]);
                colStart += SubdomainMatrices[s].NumColumns;
            }

            return result;
        }

        public ExpandedBlockColumnMatrix Transpose()
        {
            var result = new ExpandedBlockColumnMatrix(this.NumRows);
            foreach (int s in this.SubdomainMatrices.Keys)
            {
                result.SubdomainMatrices[s] = this.SubdomainMatrices[s].Transpose();
            }
            return result;
        }
    }
}
