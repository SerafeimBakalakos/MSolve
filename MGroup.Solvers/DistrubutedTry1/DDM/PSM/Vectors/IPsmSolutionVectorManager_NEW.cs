using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.LinearAlgebra;

namespace MGroup.Solvers_OLD.DistributedTry1.DDM.Psm.Vectors
{
	public interface IPsmSolutionVectorManager_NEW
	{
		DistributedOverlappingVector InterfaceProblemSolution { get; }

		void CalcSubdomainDisplacements();

		void Initialize();
	}
}
