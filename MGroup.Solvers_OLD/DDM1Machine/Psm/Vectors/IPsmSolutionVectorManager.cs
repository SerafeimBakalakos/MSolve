using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace MGroup.Solvers_OLD.DDM.Psm.Vectors
{
	public interface IPsmSolutionVectorManager
	{
		Vector GlobalBoundaryDisplacements { get; }

		void CalcSubdomainDisplacements();

		void Initialize();
	}
}
