using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;

namespace MGroup.Solvers_OLD.LinearAlgebraExtensions
{
	public static class CsrMatrixExtensions
	{
		// TODO: I also need a multithreaded version of this
		public static double MultiplyRowTimesVector(this CsrMatrix csr, int rowIdx, double[] vector)
		{
			double[] values = csr.RawValues;
			int[] colIndices = csr.RawColIndices;
			int[] rowOffsets = csr.RawRowOffsets;
			double dot = 0.0;
			int start = rowOffsets[rowIdx]; //inclusive
			int end = rowOffsets[rowIdx + 1]; //exclusive
			for (int k = start; k < end; ++k) dot += values[k] * vector[colIndices[k]];
			return dot;
		}
	}
}
