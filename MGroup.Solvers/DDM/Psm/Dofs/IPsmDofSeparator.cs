using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using MGroup.Solvers.DDM.Mappings;
using MGroup.Solvers.DofOrdering.Reordering;

namespace MGroup.Solvers.DDM.Psm.Dofs
{
	public interface IPsmDofSeparator
	{
		int GetNumBoundaryDofsCluster(int clusterID);

		DofTable GetClusterDofOrderingBoundary(int clusterID);

		DofTable GetSubdomainDofOrderingBoundary(int subdomainID);

		int[] GetSubdomainDofsBoundaryToFree(int subdomainID);

		int[] GetSubdomainDofsInternalToFree(int subdomainID);

		int GetNumFreeDofsSubdomain(int subdomainID);

		BooleanMatrixRowsToColumns GetDofMappingBoundaryClusterToSubdomain(int subdomainID);

		void MapBoundaryDofsBetweenClusterSubdomains();

		void ReorderSubdomainInternalDofs(int subdomainID, DofPermutation dofPermutation);

		void SeparateSubdomainDofsIntoBoundaryInternal();
	}
}
