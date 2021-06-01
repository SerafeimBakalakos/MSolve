using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Iterative;
using MGroup.LinearAlgebra.Distributed.Overlapping;

namespace MGroup.Solvers.DomainDecomposition.PSM.InterfaceProblem
{
	public interface IPsmInterfaceProblemMatrix
	{
		DistributedOverlappingMatrix Matrix { get; }

		void Calculate(DistributedOverlappingIndexer indexer);
	}
}
