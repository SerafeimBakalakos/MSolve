using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using MGroup.Solvers.LinearSystems;
using MGroup.Solvers.Assemblers;

namespace MGroup.Solvers.DomainDecomposition.StiffnessMatrices
{
	public class SubdomainMatrixManagerDense : ISubdomainMatrixManager
	{
		private readonly DenseMatrixAssembler assembler;
		private readonly SingleSubdomainSystem<Matrix> linearSystem;

		public SubdomainMatrixManagerDense(ISubdomain subdomain)
		{
			assembler = new DenseMatrixAssembler();
			linearSystem = new SingleSubdomainSystem<Matrix>(subdomain);
		}

		public void BuildKff(ISubdomainFreeDofOrdering dofOrdering, IEnumerable<IElement> elements,
			IElementMatrixProvider matrixProvider)
			=> linearSystem.Matrix = assembler.BuildGlobalMatrix(dofOrdering, elements, matrixProvider);

		public ILinearSystem LinearSystem => linearSystem;

		public Matrix MatrixKff => linearSystem.Matrix;

		public void SetSolution(Vector solution) => linearSystem.SolutionConcrete = solution;
	}
}
