using System.Collections.Generic;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.Assemblers;
using MGroup.Solvers.LinearSystems;

namespace MGroup.Solvers.DomainDecomposition.StiffnessMatrices
{
	public class SubdomainMatrixManagerCsr : ISubdomainMatrixManager
	{
		private readonly CsrAssembler assembler;
		private readonly SingleSubdomainSystem<CsrMatrix> linearSystem;

		public SubdomainMatrixManagerCsr(ISubdomain subdomain, bool isSymmetric)
		{
			assembler = new CsrAssembler(isSymmetric, true);
			linearSystem = new SingleSubdomainSystem<CsrMatrix>(subdomain);
		}

		public void BuildKff(ISubdomainFreeDofOrdering dofOrdering, IEnumerable<IElement> elements,
			IElementMatrixProvider matrixProvider)
			=> linearSystem.Matrix = assembler.BuildGlobalMatrix(dofOrdering, elements, matrixProvider);

		public ILinearSystem LinearSystem => linearSystem;

		public CsrMatrix MatrixKff => linearSystem.Matrix;

		public void SetSolution(Vector solution) => linearSystem.SolutionConcrete = solution;
	}
}
