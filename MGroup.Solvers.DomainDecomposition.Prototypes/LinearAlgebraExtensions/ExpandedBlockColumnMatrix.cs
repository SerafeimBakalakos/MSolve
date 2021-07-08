using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Iterative;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.LinearAlgebraExtensions
{
    public class ExpandedBlockColumnMatrix : IVectorMultipliable
    {
        public ExpandedBlockColumnMatrix(int numColumns)
        {
            NumColumns = numColumns;
        }

        public SortedDictionary<int, Matrix> SubdomainMatrices = new SortedDictionary<int, Matrix>();

        public int NumColumns { get; }

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

        public static ExpandedVector operator *(ExpandedBlockColumnMatrix matrix, Vector vector)
        {
            var result = new ExpandedVector();
            foreach (int s in matrix.SubdomainMatrices.Keys)
            {
                result.SubdomainVectors[s] = matrix.SubdomainMatrices[s] * vector;
            }
            return result;
        }

        public Matrix CopyToFullMatrix()
        {
            var result = Matrix.CreateZero(NumRows, NumColumns);
            int rowStart = 0;
            foreach (int s in SubdomainMatrices.Keys)
            {
                result.SetSubmatrix(rowStart, 0, SubdomainMatrices[s]);
                rowStart += SubdomainMatrices[s].NumRows;
            }

            return result;
        }

        public void Multiply(IVectorView lhsVector, IVector rhsVector)
        {
            var input = (Vector)lhsVector;
            var output = (ExpandedVector)rhsVector;
            output.Clear();
            foreach (int s in this.SubdomainMatrices.Keys)
            {
                output.SubdomainVectors[s] = this.SubdomainMatrices[s] * input;
            }
        }

        public IVector Multiply(IVectorView lhsVector) => this * (Vector)lhsVector;

        public ExpandedBlockRowMatrix Transpose()
        {
            var result = new ExpandedBlockRowMatrix(this.NumColumns);
            foreach (int s in this.SubdomainMatrices.Keys)
            {
                result.SubdomainMatrices[s] = this.SubdomainMatrices[s].Transpose();
            }
            return result;
        }
    }
}
