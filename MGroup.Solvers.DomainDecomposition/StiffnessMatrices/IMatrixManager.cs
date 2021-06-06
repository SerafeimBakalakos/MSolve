using System.Collections.Generic;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.LinearSystems;

namespace MGroup.Solvers.DomainDecomposition.StiffnessMatrices
{
	public interface IMatrixManager
	{
		void BuildKff(int subdomainID, ISubdomainFreeDofOrdering dofOrdering, IEnumerable<IElement> elements,
			IElementMatrixProvider matrixProvider);

		ILinearSystem GetLinearSystem(int subdomainID);

		//TODO: Refactor this
		void SetSolution(int subdomainID, Vector solution);
	}
}