using System.Collections.Generic;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.LinearSystems;

namespace MGroup.Solvers.DomainDecomposition.StiffnessMatrices
{
	public interface ISubdomainMatrixManager
	{
		void BuildKff(ISubdomainFreeDofOrdering dofOrdering, IEnumerable<IElement> elements, 
			IElementMatrixProvider matrixProvider);

		ILinearSystem LinearSystem { get; }

		//TODO: Refactor this
		void SetSolution(Vector solution);
	}
}
