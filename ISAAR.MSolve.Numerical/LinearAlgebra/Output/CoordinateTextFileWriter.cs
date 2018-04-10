﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISAAR.MSolve.Numerical.LinearAlgebra.Matrices;

namespace ISAAR.MSolve.Numerical.LinearAlgebra.Output
{
    public class CoordinateTextFileWriter: MatrixWriter
    {
        private readonly ISparseMatrix matrix;

        public CoordinateTextFileWriter(ISparseMatrix matrix)
        {
            this.matrix = matrix;
        }

        protected override void WriteToStream(StreamWriter writer)
        {
            writer.Write($"{matrix.NumRows} {matrix.NumColumns} {matrix.CountNonZeros()}");
            foreach (var (row, col, val) in matrix.EnumerateNonZeros())
            {
                writer.WriteLine();
                writer.Write($"{row} {col} {val}");
            }
        }
    }
}