using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.Distributed.LinearAlgebra;

namespace MGroup.Solvers.DDM.Psm.Vectors
{
	public interface IPsmRhsVectorManager_NEW
	{
		DistributedOverlappingVector InterfaceProblemRhs { get; }

		void CalcRhsVectors(DistributedIndexer indexer);

		void Clear();

		//TODOMPI: Not sure about exposing access to these. In a fully distributed design these will be in a DistributedVector 
		//		which will need to call DistributedVector.SumOverlappingEntries(). From that point on, these will no longer
		//		only have the stiffnesses of only 1 subdomain. Besides they are only used for testing right now. Instead I 
		//		should test the method that creates them using reflection or check the DistributedVector before (and after) 
		//		SumOverlappingEntries() is called.
		Vector GetBoundaryCondensedRhs(int subdomainID); 

		Vector GetInternalRhs(int subdomainID);
	}
}
