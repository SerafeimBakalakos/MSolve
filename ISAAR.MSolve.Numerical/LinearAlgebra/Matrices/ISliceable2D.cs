﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISAAR.MSolve.Numerical.LinearAlgebra.Vectors;

namespace ISAAR.MSolve.Numerical.LinearAlgebra.Matrices
{
    public interface ISliceable2D
    {
        Matrix Slice(int[] rowIndices, int[] colIndices);
        Matrix Slice(int rowStartInclusive, int rowEndExclusive, int colStartInclusive, int colEndExclusive);
        VectorMKL SliceColumn(int colIndex);
        VectorMKL SliceRow(int rowIndex);
    }
}