using System;
using System.Collections.Generic;
using System.Text;
using MGroup.LinearAlgebra.Distributed.Overlapping;

namespace MGroup.Solvers.DomainDecomposition.PSM.Vectors
{
	public interface IPsmSolutionVectorManager
	{
		DistributedOverlappingVector InterfaceProblemSolution { get; }

		void CalcSubdomainDisplacements();

		void Initialize(DistributedOverlappingIndexer indexer);
	}
}
