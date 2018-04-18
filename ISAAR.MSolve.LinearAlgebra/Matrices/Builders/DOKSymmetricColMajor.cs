﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ISAAR.MSolve.LinearAlgebra.Commons;
using ISAAR.MSolve.LinearAlgebra.Output;

namespace ISAAR.MSolve.LinearAlgebra.Matrices.Builders
{
    /// <summary>
    /// Use this class for building a symmetric sparse matrix, not for operations. Convert to other matrix formats once finished 
    /// and use them instead for matrix operations. Only the non zero entries of the upper triangle are stored. This class is 
    /// optimized for building global positive definite matrices, where there is at least 1 entry per column (like in FEM).
    /// </summary>
    public class DOKSymmetricColMajor : ISparseMatrix
    {
        /// <summary>
        /// An array of dictionaries is more efficent and perhaps easier to work with than a dictionary of dictionaries. There 
        /// is usually at least 1 non zero entry in each column. Otherwise this data structure wastes space, but if there were  
        /// many empty rows, perhaps another matrix format is more appropriate.
        /// To get the value: columns[colIdx][rowIdx] = value. 
        /// To get the row-value subdictionary: columns[colIdx] = Dictionary[int, double]
        /// TODO: Things to benchmark (keys = row indices): 
        /// 1) Dictionary + sort on converting: copy unordered keys and values to arrays and sort both arrays according to keys 
        /// 2) Dictionary + sort on converting: Use LINQ to sort key-value pairs and then copy each one to the arrays
        /// 3) SortedDictionary: O(log(n)) insertion but already ordered. Some entries are inserted more than once though!
        /// However this is wasteful if the sparse matrix doesn't need to be ordered. Thus I should use IDictionary and provide 
        /// static methods for Skyline (descending keys), CSC (ascending keys) or unordered (can use simple Dictionary)
        /// Perhaps a Dictionary should be used instead of SortedDictionary and only sort each column independently before 
        /// converting.
        /// </summary>
        private readonly Dictionary<int, double>[] columns;

        private DOKSymmetricColMajor(int order, Dictionary<int, double>[] columns)
        {
            this.columns = columns;
            this.NumColumns = order;
        }

        public int NumColumns { get; }
        public int NumRows { get { return NumColumns; } }

        // Perhaps I should check indices
        public double this[int rowIdx, int colIdx]
        {
            get
            {
                ProcessIndices(ref rowIdx, ref colIdx);
                if (columns[colIdx].TryGetValue(rowIdx, out double val)) return val;
                else return 0.0;
            }
            set //not thread safe
            {
                ProcessIndices(ref rowIdx, ref colIdx);
                columns[colIdx][rowIdx] = value;
            }
        }

        public static DOKSymmetricColMajor CreateEmpty(int order)
        {
            var columns = new Dictionary<int, double>[order];
            for (int j = 0; j < order; ++j) columns[j] = new Dictionary<int, double>(); //Initial capacity may be optimized.
            return new DOKSymmetricColMajor(order, columns);
        }

        public static DOKSymmetricColMajor CreateIdentity(int order)
        {
            var columns = new Dictionary<int, double>[order];
            for (int j = 0; j < order; ++j) 
            {
                var idenityCol = new Dictionary<int, double>(); //Initial capacity may be optimized.
                idenityCol[j] = 1.0;
                columns[j] = idenityCol; 
            }
            return new DOKSymmetricColMajor(order, columns);
        }

        #region global matrix building 
        /// <summary>
        /// If the entry already exists: the new value is added to the existing one: this[rowIdx, colIdx] += value. 
        /// Otherwise the entry is inserted with the new value: this[rowIdx, colIdx] = value.
        /// Use this method instead of this[rowIdx, colIdx] += value, as it is an optimized version.
        /// Warning: Be careful not to add to both an entry and its symmetric, unless this is what you want.
        /// </summary>
        /// <param name="rowIdx"></param>
        /// <param name="colIdx"></param>
        /// <param name="value"></param>
        public void AddToEntry(int rowIdx, int colIdx, double value)
        {
            ProcessIndices(ref rowIdx, ref colIdx);
            if (columns[colIdx].TryGetValue(rowIdx, out double oldValue))
            {
                columns[colIdx][rowIdx] = value + oldValue;
            }
            else columns[colIdx][rowIdx] = value;
            //The Dictionary columns[rowIdx] is indexed twice in both cases. Is it possible to only index it once?
        }

        /// <summary>
        /// Use this method if 1) both the global DOK and the element matrix are symmetric and 2) the rows and columns correspond
        /// to the same degrees of freedom. The caller is responsible for making sure that both matrices are symmetric and that 
        /// the dimensions and dofs of the element matrix and dof mappings match.
        /// </summary>
        /// <param name="elementMatrix">The element submatrix, entries of which will be added to the global DOK. It must be 
        ///     symmetric and its <see cref="IIndexable2D.NumColumns"/> = <see cref="IIndexable2D.NumRows"/> must be equal to
        ///     elemenDofs.Length = globalDofs.Length.</param>
        /// <param name="elementDofs">The entries in the element matrix to be added to the global matrix. Specificaly, pairs of 
        ///     (elementDofs[i], elementDofs[j]) will be added to (globalDofs[i], globalDofs[j]).</param>
        /// <param name="globalDofs">The entries in the global matrix where element matrix entries will be added to. Specificaly,
        ///     pairs of (elementDofs[i], elementDofs[j]) will be added to (globalDofs[i], globalDofs[j]).</param>
        public void AddSubmatrixSymmetric(IIndexable2D elementMatrix, int[] elementDofs, int[] globalDofs)
        {
            int n = elementDofs.Length;
            for (int j = 0; j < n; ++j)
            {
                int elementCol = elementDofs[j];
                int globalCol = globalDofs[j];

                //Diagonal entry
                if (columns[globalCol].TryGetValue(globalCol, out double oldGlobalDiagValue))
                {
                    columns[globalCol][globalCol] = elementMatrix[elementCol, elementCol] + oldGlobalDiagValue;
                }
                else columns[globalCol][globalCol] = elementMatrix[elementCol, elementCol];

                //Non diagonal entries
                for (int i = 0; i <= j; ++i)
                {
                    int elementRow = elementDofs[j];
                    int globalRow = globalDofs[j];
                    double newGlobalValue = elementMatrix[elementRow, elementCol];
                    if (columns[globalCol].TryGetValue(globalRow, out double oldGlobalValue))
                    {
                        newGlobalValue += oldGlobalValue;
                    }
                    columns[globalCol][globalRow] = newGlobalValue;
                }
            }
        }

        /// <summary>
        /// Use this method if you are sure that the all relevant global dofs are above or on the diagonal.
        /// </summary>
        public void AddSubmatrixAboveDiagonal(IIndexable2D elementMatrix,
            IReadOnlyDictionary<int, int> elementRowsToGlobalRows, IReadOnlyDictionary< int, int> elementColsToGlobalCols)
        { 
            foreach (var colPair in elementColsToGlobalCols) // Col major ordering
            {
                int elementCol = colPair.Key;
                int globalCol = colPair.Value;
                foreach (var rowPair in elementRowsToGlobalRows)
                { 
                    int elementRow = rowPair.Key;
                    int globalRow = rowPair.Value;
                    double elementValue = elementMatrix[elementRow, elementCol];
                    if (columns[globalCol].TryGetValue(globalRow, out double oldGlobalValue))
                    {
                        columns[globalCol][globalRow] = elementValue + oldGlobalValue;
                    }
                    else columns[globalCol][globalRow] = elementValue;
                }
            }
        }

        /// <summary>
        /// Use this method if you are sure that the all relevant global dofs are under or on the diagonal of this matrix.
        /// </summary>
        public void AddSubmatrixUnderDiagonal(IIndexable2D elementMatrix,
            IReadOnlyDictionary<int, int> elementRowsToGlobalRows, IReadOnlyDictionary<int, int> elementColsToGlobalCols)
        {
            foreach (var rowPair in elementRowsToGlobalRows) // Transpose(Col major ordering) = Row major ordering
            {
                int elementRow = rowPair.Key;
                int globalRow = rowPair.Value;
                foreach (var colPair in elementColsToGlobalCols)
                {
                    int elementCol = colPair.Key;
                    int globalCol = colPair.Value;
                    double elementValue = elementMatrix[elementRow, elementCol];
                    // Transpose the access on the global matrix only. 
                    // This is equivalent to calling the indexer for each lower triangle entry, but without the explicit swap.
                    if (columns[globalRow].TryGetValue(globalCol, out double oldGlobalValue))
                    {
                        columns[globalRow][globalCol] = elementValue + oldGlobalValue;
                    }
                    else columns[globalRow][globalCol] = elementValue;
                }
            }
        }

        /// <summary>
        /// Only the global dofs above the diagonal of this matrix will be added. Use this for symmetric submatrices that are 
        /// mapped to symmetric global dofs (e.g. in FEM).
        /// </summary>
        public void AddUpperPartOfSubmatrix(IIndexable2D elementMatrix,
            IReadOnlyDictionary<int, int> elementRowsToGlobalRows, IReadOnlyDictionary<int, int> elementColsToGlobalCols)
        {
            /* 
             * TODO: Ideally the Dictionaries would be replaced with 2 arrays (or their container e.g. BiList) for 
             * faster look-up. The arrays should be sorted on the global dofs. Then I could decide if a submatrix is above, under 
             * or crossing the diagonal and only expose a single AddSubmatrix() method. This would call the relevant private 
             * method. Perhaps I can also get rid of all the ifs in this method. Perhaps I also could get rid of the ifs in the 
             * crossing case by setting by using correct loops.  
             */
            foreach (var colPair in elementColsToGlobalCols) // Col major ordering
            {
                int elementCol = colPair.Key;
                int globalCol = colPair.Value;
                foreach (var rowPair in elementRowsToGlobalRows)
                {
                    int globalRow = rowPair.Value;
                    if (globalRow <= globalCol)
                    {
                        int elementRow = rowPair.Key;
                        double elementValue = elementMatrix[elementRow, elementCol];
                        if (columns[globalCol].TryGetValue(globalRow, out double oldGlobalValue))
                        {
                            columns[globalCol][globalRow] = elementValue + oldGlobalValue;
                        }
                        else columns[globalCol][globalRow] = elementValue;
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Creates a CSC matrix, containing only the upper triangular entries.
        /// </summary>
        /// <param name="sortRowsOfEachCol">True to sort the row indices of the CSC matrix between colOffsets[j] and 
        ///     colOffsets[j+1] in ascending order. False to leave them unordered. Ordered rows might result in better  
        ///     performance during multiplications or they might be required by 3rd party libraries. Leaving them unordered will 
        ///     be faster during creation of the CSC matrix.</param>
        /// <returns></returns>
        public SymmetricCSC BuildSymmetricCSCMatrix(bool sortRowsOfEachCol)
        {
            (double[] values, int[] rowIndices, int[] colOffsets) = BuildSymmetricCSCArrays(sortRowsOfEachCol);
            return new SymmetricCSC(values, rowIndices, colOffsets, false);
        }

        public int CountNonZeros()
        {
            int count = 0;
            for (int j = 0; j < NumColumns; ++j)
            {
                foreach (var rowVal in columns[j])
                {
                    if (rowVal.Key == j) ++count;
                    else count += 2; //Each upper triangle entries haa a corresponding lower triangle entry.
                }
            }
            return count;
        }

        public IEnumerable<(int row, int col, double value)> EnumerateNonZeros()
        {
            for (int j = 0; j < NumColumns; ++j)
            {
                foreach (var rowVal in columns[j])
                {
                    if (rowVal.Key == j) yield return (rowVal.Key, j, rowVal.Value);
                    else //Each upper triangle entries haa a corresponding lower triangle entry.
                    {
                        yield return (rowVal.Key, j, rowVal.Value);
                        yield return (j, rowVal.Key, rowVal.Value);
                    }
                }
            }
        }

        public bool Equals(IIndexable2D other, double tolerance = 1e-13)
        {
            return DenseStrategies.AreEqual(this, other, tolerance);
        }

        //Perhaps there should be a dedicated symmetric CSC format, identical to CSC.
        public SparseFormat GetSparseFormat()
        {
            (double[] values, int[] rowIndices, int[] colOffsets) = BuildSymmetricCSCArrays(false);
            var format = new SparseFormat();
            format.RawValuesTitle = "Values";
            format.RawValuesArray = values;
            format.RawIndexArrays.Add("Row indices", rowIndices);
            format.RawIndexArrays.Add("Column offsets", colOffsets);
            return format;
        }

        

        /// <summary>
        /// Perhaps this should be manually inlined. Testing needed.
        /// </summary>
        /// <param name="rowIdx"></param>
        /// <param name="colIdx"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ProcessIndices(ref int rowIdx, ref int colIdx)
        {
            if (rowIdx > colIdx)
            {
                int swap = rowIdx;
                rowIdx = colIdx;
                colIdx = swap;
            }
        }

        /// <summary>
        /// Creates the CSC arrays, containing only the upper triangular entries.
        /// </summary>
        /// <param name="sortRowsOfEachCol">True to sort the row indices of the CSC matrix between colOffsets[j] and 
        ///     colOffsets[j+1] in ascending order. False to leave them unordered. Ordered rows might result in better  
        ///     performance during multiplications or they might be required by 3rd party libraries. Leaving them unordered will 
        ///     be faster during creation of the CSC matrix.</param>
        /// <returns></returns>
        private (double[] values, int[] rowIndices, int[] columnOffsets) BuildSymmetricCSCArrays(bool sortRowsOfEachCol)
        {
            int[] colOffsets = new int[NumColumns + 1];
            int nnz = 0;
            for (int j = 0; j < NumColumns; ++j)
            {
                colOffsets[j] = nnz;
                nnz += columns[j].Count;
            }
            colOffsets[NumColumns] = nnz; //The last CSC entry is nnz.

            int[] rowIndices = new int[nnz];
            double[] values = new double[nnz];
            int counter = 0;
            for (int j = 0; j < NumColumns; ++j)
            {
                if (sortRowsOfEachCol)
                {
                    foreach (var rowVal in columns[j].OrderBy(pair => pair.Key))
                    {
                        rowIndices[counter] = rowVal.Key;
                        values[counter] = rowVal.Value;
                        ++counter;
                    }
                }
                else
                {
                    foreach (var rowVal in columns[j])
                    {
                        rowIndices[counter] = rowVal.Key;
                        values[counter] = rowVal.Value;
                        ++counter;
                    }
                }
            }

            return (values, rowIndices, colOffsets);
        }
    }
}