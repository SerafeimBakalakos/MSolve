using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.DDM.Psm.InterfaceProblem;

namespace MGroup.Solvers.DDM.Psm.Preconditioner
{
	public class PsmPreconditionerIdentity : IPsmPreconditioner
	{

		public PsmPreconditionerIdentity()
		{
		}

		public void Calculate(IInterfaceProblemMatrix interfaceProblemMatrix) 
		{
		}

		public void SolveLinearSystem(IVectorView rhsVector, IVector lhsVector)
		{
			lhsVector.CopyFrom(rhsVector);
		}
	}
}
