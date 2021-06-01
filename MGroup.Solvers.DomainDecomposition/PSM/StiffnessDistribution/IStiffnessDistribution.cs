using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using MGroup.Solvers.DomainDecomposition.Mappings;
using MGroup.LinearAlgebra.Distributed.Overlapping;

namespace MGroup.Solvers.DomainDecomposition.PSM.StiffnessDistribution
{
	public interface IStiffnessDistribution
	{
		void CalcSubdomainScaling(DistributedOverlappingIndexer indexer);

		//TODOMPI: These need rework at the equation level in the distributed logic. I should probably use scaling matrices without any mapping 
		///// <summary>
		///// In theory these matrices are called Lpb.
		///// </summary>
		//IMappingMatrix GetDofMappingBoundaryClusterToSubdomain(int subdomainID);

		Dictionary<int, SparseVector> DistributeNodalLoads(Table<INode, IDofType, double> nodalLoads);

		//TODOMPI: remove this. It is only needed to convert global force vectors to subdomain force vectors. 
		//		In the current design global vectors are to be avoided.
		void ScaleForceVector(int subdomainID, Vector subdomainForces); 
	}
}
