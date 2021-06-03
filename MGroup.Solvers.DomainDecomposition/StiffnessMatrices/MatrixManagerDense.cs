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
	public class MatrixManagerDense : IMatrixManager
	{
		private readonly Dictionary<int, DenseMatrixAssembler> assemblers = new Dictionary<int, DenseMatrixAssembler>();
		private readonly Dictionary<int, SingleSubdomainSystem<Matrix>> linearSystems =
			new Dictionary<int, SingleSubdomainSystem<Matrix>>();

		public MatrixManagerDense(IStructuralModel model)
		{
			foreach (ISubdomain subdomain in model.Subdomains)
			{
				linearSystems[subdomain.ID] = new SingleSubdomainSystem<Matrix>(subdomain);
				assemblers[subdomain.ID] = new DenseMatrixAssembler();
			}
		}

		public IMatrix BuildKff(int subdomainID, ISubdomainFreeDofOrdering dofOrdering, IEnumerable<IElement> elements,
			IElementMatrixProvider matrixProvider)
		{
			var linearSystem = linearSystems[subdomainID];
			linearSystem.Matrix = assemblers[subdomainID].BuildGlobalMatrix(dofOrdering,
				linearSystem.Subdomain.Elements, matrixProvider);
			return linearSystem.Matrix;
		}

		public ILinearSystem GetLinearSystem(int subdomainID) => linearSystems[subdomainID];

		public Matrix GetMatrixKff(int subdomainID) => linearSystems[subdomainID].Matrix;

		public void SetSolution(int subdomainID, Vector solution)
		{
			linearSystems[subdomainID].SolutionConcrete = solution;
		}
	}
}
