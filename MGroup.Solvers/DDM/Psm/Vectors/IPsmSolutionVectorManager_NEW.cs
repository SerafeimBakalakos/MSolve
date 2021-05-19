using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.Distributed.LinearAlgebra;

namespace MGroup.Solvers.DDM.Psm.Vectors
{
	public interface IPsmSolutionVectorManager_NEW
	{
		DistributedOverlappingVector InterfaceProblemSolution { get; }

		void CalcSubdomainDisplacements();

		void Initialize();
	}
}
