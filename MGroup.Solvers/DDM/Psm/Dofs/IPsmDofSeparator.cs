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
		int GetClusterNumBoundaryDofs(int clusterID);

		DofTable GetClusterDofOrderingBoundary(int clusterID);

		DofTable GetDofOrderingBoundary(int subdomainID);

		int[] GetDofsBoundaryToFree(int subdomainID);

		int[] GetDofsInternalToFree(int subdomainID);

		int GetNumFreeDofs(int subdomainID);

		BooleanMatrixRowsToColumns GetDofMappingBoundaryClusterToSubdomain(int subdomainID);

		void MapBoundaryDofs();

		void ReorderInternalDofs(int subdomainID, DofPermutation dofPermutation);

		void SeparateBoundaryInternalDofs();
	}
}
