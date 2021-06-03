using System.Collections.Generic;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.Assemblers;
using MGroup.Solvers.LinearSystems;

namespace MGroup.Solvers.DomainDecomposition.StiffnessMatrices
{
	public class MatrixManagerCscSymmetric : IMatrixManager
	{
		private readonly Dictionary<int, SymmetricCscAssembler> assemblers = new Dictionary<int, SymmetricCscAssembler>();
		private readonly Dictionary<int, SingleSubdomainSystem<SymmetricCscMatrix>> linearSystems =
			new Dictionary<int, SingleSubdomainSystem<SymmetricCscMatrix>>();

		public MatrixManagerCscSymmetric(IStructuralModel model)
		{
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				linearSystems[subdomain.ID] = new SingleSubdomainSystem<SymmetricCscMatrix>(subdomain);
				assemblers[subdomain.ID] = new SymmetricCscAssembler();
			}
		}

		public IMatrix BuildKff(int subdomainID, ISubdomainFreeDofOrdering dofOrdering, IEnumerable<IElement> elements,
			IElementMatrixProvider matrixProvider)
		{
			var linearSystem = linearSystems[subdomainID];
			linearSystem.Matrix =
				assemblers[subdomainID].BuildGlobalMatrix(dofOrdering, linearSystem.Subdomain.Elements, matrixProvider);
			return linearSystem.Matrix;
		}

		public ILinearSystem GetLinearSystem(int subdomainID) => linearSystems[subdomainID];

		public SymmetricCscMatrix GetMatrixKff(int subdomainID) => linearSystems[subdomainID].Matrix;

		public void SetSolution(int subdomainID, Vector solution)
		{
			linearSystems[subdomainID].SolutionConcrete = solution;
		}
	}
}
