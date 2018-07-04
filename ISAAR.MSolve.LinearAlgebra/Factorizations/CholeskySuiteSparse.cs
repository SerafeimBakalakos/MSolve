﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISAAR.MSolve.LinearAlgebra.Exceptions;
using ISAAR.MSolve.LinearAlgebra.Matrices.Builders;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.LinearAlgebra.SuiteSparse;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Commons;

//TODO: SuiteSparse Common should be represented here by an IDisposable class SuiteSparseCommon.
namespace ISAAR.MSolve.LinearAlgebra.Factorizations
{
    public class CholeskySuiteSparse : IFactorization, IDisposable
    {
        private IntPtr common;
        private IntPtr factorizedMatrix;

        private CholeskySuiteSparse(int order, IntPtr suiteSparseCommon, IntPtr factorizedMatrix)
        {
            this.Order = order;
            this.common = suiteSparseCommon;
            this.factorizedMatrix = factorizedMatrix;
        }

        ~CholeskySuiteSparse()
        {
            ReleaseResources();
        }

        /// <summary>
        /// The number of rows or columns of the matrix. 
        /// </summary>
        public int Order { get; }

        public int NumNonZeros
        {
            get
            {
                return SuiteSparseUtilities.GetFactorNonZeros(factorizedMatrix);
            }
        }

        public static CholeskySuiteSparse Factorize(int order, int nonZerosUpper, double[] values, int[] rowIndices,
            int[] colOffsets, bool superNodal, SuiteSparseOrdering ordering)
        {
            int factorizationType = superNodal ? 1 : 0;
            IntPtr common = SuiteSparseUtilities.CreateCommon(factorizationType, (int)ordering);
            if (common == IntPtr.Zero) throw new SuiteSparseException("Failed to initialize SuiteSparse.");
            int status = SuiteSparseUtilities.FactorizeCSCUpper(order, nonZerosUpper, values, rowIndices, colOffsets,
                out IntPtr factorizedMatrix, common);
            if (status == -2)
            {
                SuiteSparseUtilities.DestroyCommon(ref common);
                throw new SuiteSparseException("Factorization did not succeed. This could be caused by insufficent memory,"
                    + " due to excessive fill-in.");
            }
            else if (status >= 0)
            {
                SuiteSparseUtilities.DestroyCommon(ref common);
                throw new SuiteSparseException("The matrix not being positive definite."
                    + $" Cholesky failed at column {status} (0-based indexing).");
            }
            else return new CholeskySuiteSparse(order, common, factorizedMatrix);
        }

        /// <summary>
        /// Update row (same as column) <paramref name="rowIdx"/> of the factorized matrix to the one it would have if 
        /// <paramref name="newRow"/> was set as the <paramref name="rowIdx"/>-th row/col of the original matrix and then the 
        /// factorization was computed. The existing <paramref name="rowIdx"/>-th row/column of the original matrix must be equal 
        /// to the <paramref name="rowIdx"/>-th row/col of the identity matrix. 
        /// </summary>
        /// <param name="rowIdx"></param>
        /// <param name="newRow"></param>
        public void AddRow(int rowIdx, SparseVector newRow) //TODO: The row should be input as a sparse CSC matrix with dimensions order-by-1
        {
            //TODO: use Preconditions for these tests and implement IIndexable2D.
            if ((rowIdx < 0) || (rowIdx >= Order))
            {
                throw new IndexOutOfRangeException($"Cannot access row {rowIdx} in a"
                    + $" {Order}-by-{Order} matrix");
            }
            if (newRow.Length != Order)
            {
                throw new NonMatchingDimensionsException($"The new row/column must have the same number of rows as this"
                    + $"{Order}-by-{Order} factorized matrix, but was {newRow.Length}-by-1");
            }

            int nnz = newRow.CountNonZeros();
            int[] colOffsets = { 0, nnz };
            int status = SuiteSparseUtilities.RowAdd(Order, factorizedMatrix, rowIdx,
                nnz, newRow.InternalValues, newRow.InternalIndices, colOffsets, common);
            if (status != 1)
            {
                throw new SuiteSparseException("Rows addition did not succeed. This could be caused by insufficent memory");
            }
        }

        public Vector BackSubstitution(Vector rhs) { return SolveInternal(SystemType.BackSubstitution, rhs); }
        public Matrix BackSubstitution(Matrix rhs) { return SolveInternal(SystemType.BackSubstitution, rhs); }

        public double CalcDeterminant()
        {
            if (factorizedMatrix == IntPtr.Zero)
            {
                throw new AccessViolationException("The factorized matrix has been freed from unmanaged memory");
            }
            throw new NotImplementedException();
        }

        /// <summary>
        /// Update row (same as column) <paramref name="rowIdx"/> of the factorized matrix to the one it would have if the
        /// <paramref name="rowIdx"/>-th row/col of the identity matrix was set as the <paramref name="rowIdx"/>-th row/col of  
        /// the original matrix and then the factorization was computed.
        /// </summary>
        /// <param name="rowIdx"></param>
        public void DeleteRow(int rowIdx)
        {
            int status = SuiteSparseUtilities.RowDelete(factorizedMatrix, rowIdx, common);
            if (status != 1)
            {
                throw new SuiteSparseException("Rows deletion did not succeed.");
            }
        }

        public void Dispose()
        {
            ReleaseResources();
            GC.SuppressFinalize(this);
        }

        public Vector ForwardSubstitution(Vector rhs) { return SolveInternal(SystemType.ForwardSubstitution, rhs); }
        public Matrix ForwardSubstitution(Matrix rhs) { return SolveInternal(SystemType.ForwardSubstitution, rhs); }
        public Vector SolveLinearSystem(Vector rhs) { return SolveInternal(SystemType.Regular, rhs); }
        public Matrix SolveLinearSystem(Matrix rhs) { return SolveInternal(SystemType.Regular, rhs); }

        /// <summary>
        /// Perhaps I should use SafeHandle (thread safety, etc). 
        /// Also perhaps there should be dedicated objects for closing each handle.
        /// </summary>
        private void ReleaseResources() 
        {
            if (common != IntPtr.Zero)
            {
                // Supposedly throwing in destructors and Dispose() is poor practice.
                if (factorizedMatrix == IntPtr.Zero) 
                {
                    throw new AccessViolationException("The matrix in unmanaged memory has already been cleared or lost");
                }
                SuiteSparseUtilities.DestroyFactor(ref factorizedMatrix, common);
                factorizedMatrix = IntPtr.Zero;
                SuiteSparseUtilities.DestroyCommon(ref common);
                common = IntPtr.Zero;
            }
        }

        private Vector SolveInternal(SystemType system, Vector rhs)
        {
            if (factorizedMatrix == IntPtr.Zero)
            {
                throw new AccessViolationException("The factorized matrix has been freed from unmanaged memory");
            }
            Preconditions.CheckSystemSolutionDimensions(Order, Order, rhs.Length);
            double[] solution = new double[rhs.Length];
            int status = SuiteSparseUtilities.Solve((int)system, Order, 1, factorizedMatrix, rhs.InternalData, solution, common);
            if (status != 1) throw new SuiteSparseException("System solution failed.");
            return Vector.CreateFromArray(solution, false);
        }

        private Matrix SolveInternal(SystemType system, Matrix rhs)
        {
            if (factorizedMatrix == IntPtr.Zero)
            {
                throw new AccessViolationException("The factorized matrix has been freed from unmanaged memory");
            }
            Preconditions.CheckSystemSolutionDimensions(Order, Order, rhs.NumRows);
            double[] solution = new double[rhs.NumRows * rhs.NumColumns];
            int status = SuiteSparseUtilities.Solve((int)system, Order, rhs.NumColumns, factorizedMatrix, rhs.InternalData,
                solution, common);
            if (status != 1) throw new SuiteSparseException("System solution failed.");
            return Matrix.CreateFromArray(solution, rhs.NumRows, rhs.NumColumns, false);
        }
    }
}