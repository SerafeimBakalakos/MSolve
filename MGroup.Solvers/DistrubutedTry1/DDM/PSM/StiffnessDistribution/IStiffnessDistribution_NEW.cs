using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers_OLD.DDM.Mappings;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.FreedomDegrees;

namespace MGroup.Solvers_OLD.DistributedTry1.DDM.Psm.StiffnessDistribution
{
	public interface IStiffnessDistribution_NEW
	{
		void CalcSubdomainScaling();

		/// <summary>
		/// In theory these matrices are called Lpb.
		/// </summary>
		IMappingMatrix GetDofMappingBoundaryClusterToSubdomain(int subdomainID);

		Dictionary<int, SparseVector> DistributeNodalLoads(Table<INode, IDofType, double> nodalLoads);

		//TODOMPI: remove this. It is only needed to convert global force vectors to subdomain force vectors. 
		//		In the current design global vectors are to be avoided.
		void ScaleForceVector(int subdomainID, Vector subdomainForces); 
	}
}
