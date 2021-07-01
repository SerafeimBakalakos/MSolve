using System.Collections.Generic;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.Assemblers;
using MGroup.Solvers.LinearSystems;

namespace MGroup.Solvers.DomainDecomposition.StiffnessMatrices
{
	public class SubdomainMatrixManagerSymmetricCsc : ISubdomainMatrixManager
	{
		private readonly SymmetricCscAssembler assembler;
		private readonly SingleSubdomainSystem<SymmetricCscMatrix> linearSystem;

		public SubdomainMatrixManagerSymmetricCsc(ISubdomain subdomain)
		{
			assembler = new SymmetricCscAssembler(true);
			linearSystem = new SingleSubdomainSystem<SymmetricCscMatrix>(subdomain);
		}

		public void BuildKff(ISubdomainFreeDofOrdering dofOrdering, IEnumerable<IElement> elements,
			IElementMatrixProvider matrixProvider)
			=> linearSystem.Matrix = assembler.BuildGlobalMatrix(dofOrdering, elements, matrixProvider);

		public ILinearSystem LinearSystem => linearSystem;

		public SymmetricCscMatrix MatrixKff => linearSystem.Matrix;

		public void SetSolution(Vector solution) => linearSystem.SolutionConcrete = solution;
	}
}
