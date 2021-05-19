using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.Solvers_OLD.DDM.Mappings;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.FreedomDegrees;

namespace MGroup.Solvers_OLD.DDM.Psm.StiffnessDistribution
{
	public interface IStiffnessDistribution
	{
		void CalcSubdomainScaling();

		/// <summary>
		/// In theory these matrices are called Lpb.
		/// </summary>
		IMappingMatrix GetDofMappingBoundaryClusterToSubdomain(int subdomainID);

		Dictionary<int, SparseVector> DistributeNodalLoads(Table<INode, IDofType, double> globalNodalLoads,
			IEnumerable<ISubdomain> subdomains);

		void ScaleForceVector(int subdomainID, Vector subdomainForces);
	}
}
