using System.Collections.Generic;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.Assemblers;
using MGroup.Solvers.LinearSystems;

namespace MGroup.Solvers.DomainDecomposition.StiffnessMatrices
{
	public class MatrixManagerCsr : IMatrixManager
	{
		private readonly Dictionary<int, CsrAssembler> assemblers = new Dictionary<int, CsrAssembler>();
		private readonly Dictionary<int, SingleSubdomainSystem<CsrMatrix>> linearSystems =
			new Dictionary<int, SingleSubdomainSystem<CsrMatrix>>();

		public MatrixManagerCsr(IStructuralModel model, bool isSymmetric)
		{
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				linearSystems[subdomain.ID] = new SingleSubdomainSystem<CsrMatrix>(subdomain);
				assemblers[subdomain.ID] = new CsrAssembler(isSymmetric);
			}
		}

		public void BuildKff(int subdomainID, ISubdomainFreeDofOrdering dofOrdering, IEnumerable<IElement> elements,
			IElementMatrixProvider matrixProvider)
		{
			var linearSystem = linearSystems[subdomainID];
			linearSystem.Matrix = assemblers[subdomainID].BuildGlobalMatrix(
				dofOrdering, linearSystem.Subdomain.Elements, matrixProvider);
		}

		public ILinearSystem GetLinearSystem(int subdomainID) => linearSystems[subdomainID];

		public CsrMatrix GetMatrixKff(int subdomainID) => linearSystems[subdomainID].Matrix;

		public void SetSolution(int subdomainID, Vector solution)
		{
			linearSystems[subdomainID].SolutionConcrete = solution;
		}
	}
}
