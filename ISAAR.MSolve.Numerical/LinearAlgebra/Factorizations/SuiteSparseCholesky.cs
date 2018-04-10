﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISAAR.MSolve.Numerical.Exceptions;
using ISAAR.MSolve.Numerical.LinearAlgebra.Commons;
using ISAAR.MSolve.Numerical.LinearAlgebra.Vectors;

namespace ISAAR.MSolve.Numerical.LinearAlgebra.Factorizations
{
    public class SuiteSparseCholesky : IFactorization, IDisposable
    {
        private IntPtr common;
        private IntPtr factorizedMatrix;

        private SuiteSparseCholesky(int order, IntPtr suiteSparseCommon, IntPtr factorizedMatrix)
        {
            this.Order = order;
            this.common = suiteSparseCommon;
            this.factorizedMatrix = factorizedMatrix;
        }

        ~SuiteSparseCholesky()
        {
            ReleaseResources();
        }

        /// <summary>
        /// The number of rows or columns of the matrix. 
        /// </summary>
        public int Order { get; }

        public static SuiteSparseCholesky CalcFactorization(int order, int nonZerosUpper, double[] values, int[] rowIndices,
            int[] colOffsets)
        {
            IntPtr common = SuiteSparseUtilities.CreateCommon();
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
            else return new SuiteSparseCholesky(order, common, factorizedMatrix);
        }

        public double CalcDeterminant()
        {
            if (factorizedMatrix == IntPtr.Zero)
            {
                throw new AccessViolationException("The factorized matrix has been freed from unmanaged memory");
            }
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            ReleaseResources();
            GC.SuppressFinalize(this);
        }

        public VectorMKL SolveLinearSystem(VectorMKL rhs)
        {
            if (factorizedMatrix == IntPtr.Zero)
            {
                throw new AccessViolationException("The factorized matrix has been freed from unmanaged memory");
            }
            double[] solution = new double[rhs.Length];
            SuiteSparseUtilities.Solve(Order, factorizedMatrix, rhs.InternalData, solution, common);
            return VectorMKL.CreateFromArray(solution, false);
        }

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
    }
}