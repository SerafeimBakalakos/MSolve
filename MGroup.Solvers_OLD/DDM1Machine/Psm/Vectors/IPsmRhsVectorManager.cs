using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace MGroup.Solvers_OLD.DDM.Psm.Vectors
{
	public interface IPsmRhsVectorManager
	{
		Vector InterfaceProblemRhs { get; }

		void CalcRhsVectors();

		void Clear();

		Vector GetBoundaryCondensedRhs(int subdomainID);

		Vector GetInternalRhs(int subdomainID);
	}
}
