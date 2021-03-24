using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using MGroup.Solvers.DDM.Mappings;
using MGroup.Solvers.DofOrdering.Reordering;

namespace MGroup.Solvers.DDM.FetiDP.Dofs
{
	public interface IFetiDPDofSeparator
	{
		DofTable GlobalCornerDofOrdering { get; }

		int NumGlobalCornerDofs { get; }

		DofTable GetDofOrderingCorner(int subdomainID);

		int[] GetDofsBoundaryRemainderToRemainder(int subdomain);

		int[] GetDofsCornerToFree(int subdomainID);

		int[] GetDofsInternalToRemainder(int subdomainID);

		int[] GetDofsRemainderToFree(int subdomainID);

		int GetNumFreeDofs(int subdomainID);

		BooleanMatrixRowsToColumns GetDofMappingCornerGlobalToSubdomain(int subdomainID);

		void MapCornerDofs();

		void OrderGlobalCornerDofs();

		void ReorderGlobalCornerDofs(DofPermutation permutation);

		void ReorderRemainderDofs(int subdomainID, DofPermutation permutation);

		void SeparateBoundaryRemainderAndInternalDofs();

		void SeparateCornerRemainderDofs(ICornerDofSelection cornerDofSelection);
	}
}
