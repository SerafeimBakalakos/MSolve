using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.LinearAlgebra.Distributed.Overlapping;

namespace MGroup.Solvers.DomainDecomposition.PSM.Vectors
{
	public interface IPsmRhsVectorManager
	{
		DistributedOverlappingVector InterfaceProblemRhs { get; }

		void CalcRhsVectors(DistributedOverlappingIndexer indexer);

		void Clear();

		Vector GetInternalRhs(int subdomainID);
	}
}
