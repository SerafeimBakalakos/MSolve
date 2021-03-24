using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.DDM.Psm.InterfaceProblem;

namespace MGroup.Solvers.DDM.Psm.Preconditioner
{
	public class PsmPreconditionerDirect : IPsmPreconditioner
	{
		private Matrix inverseInterfaceProblemMatrix;

		public PsmPreconditionerDirect()
		{
		}

		public void Calculate(IInterfaceProblemMatrix interfaceProblemMatrix)
		{
			int size = interfaceProblemMatrix.NumRows;
			var matrixCol = Vector.CreateZero(size);
			inverseInterfaceProblemMatrix = Matrix.CreateZero(size, size);
			for (int i = 0; i < size; ++i)
			{
				var identityCol = Vector.CreateZero(size);
				identityCol[i] = 1.0;
				interfaceProblemMatrix.Multiply(identityCol, matrixCol);
				inverseInterfaceProblemMatrix.SetSubcolumn(i, matrixCol, 0);
			}
			inverseInterfaceProblemMatrix.InvertInPlace();
		}

		public void SolveLinearSystem(IVectorView rhsVector, IVector lhsVector)
		{
			inverseInterfaceProblemMatrix.MultiplyIntoResult(rhsVector, lhsVector);
		}
	}
}
