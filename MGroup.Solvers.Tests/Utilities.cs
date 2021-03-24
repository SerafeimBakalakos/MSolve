namespace MGroup.Solvers.Tests
{
	using System;
	using ISAAR.MSolve.LinearAlgebra.Matrices;
	using ISAAR.MSolve.LinearAlgebra.Vectors;
	using Xunit;

	public static class Utilities
    {
        public static void AssertEqual(int[] expected, int[] computed)
        {
            Assert.Equal(expected.Length, computed.Length);
            for (int i = 0; i < expected.Length; ++i) Assert.Equal(expected[i], computed[i]);
        }

		public static Matrix CreateExplicitMatrix(int numRows, int numCols, Func<Vector, Vector> matrixVectorMultiply)
		{
			Matrix denseA = Matrix.CreateZero(numRows, numCols);
			for (int j = 0; j < numCols; ++j)
			{
				var colIdentity = Vector.CreateZero(numCols);
				colIdentity[j] = 1.0;
				Vector colA = matrixVectorMultiply(colIdentity);
				denseA.SetSubcolumn(j, colA);
			}
			return denseA;
		}
	}
}
