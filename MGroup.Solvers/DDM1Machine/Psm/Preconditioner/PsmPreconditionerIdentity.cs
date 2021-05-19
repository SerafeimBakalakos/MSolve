using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers_OLD.DDM.Psm.InterfaceProblem;

namespace MGroup.Solvers_OLD.DDM.Psm.Preconditioner
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
