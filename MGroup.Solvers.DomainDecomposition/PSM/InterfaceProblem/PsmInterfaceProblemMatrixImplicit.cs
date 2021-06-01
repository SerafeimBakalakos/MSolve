using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Environments;
using MGroup.LinearAlgebra.Distributed.Overlapping;
using MGroup.Solvers.DomainDecomposition.PSM.Dofs;
using MGroup.Solvers.DomainDecomposition.PSM.StiffnessMatrices;

namespace MGroup.Solvers.DomainDecomposition.PSM.InterfaceProblem
{
	/// <summary>
	/// Each subdomain's matrix is implicitly represented by an expression invovling multiple intermediate matrices, 
	/// all refering to the same subdomain. Taking advantage of the distributed property of matrix-vector multiplication, 
	/// multiplying each subdomain's "matrix" with a vector is performed by multiplying the intermediate matrices with that 
	/// vector and reducing the intermediate vector results.
	/// </summary>
	public class PsmInterfaceProblemMatrixImplicit : IPsmInterfaceProblemMatrix
	{
		private readonly IComputeEnvironment environment;
		private readonly IPsmMatrixManager matrixManager;

		public PsmInterfaceProblemMatrixImplicit(IComputeEnvironment environment, IPsmMatrixManager matrixManager)
		{
			this.environment = environment;
			this.matrixManager = matrixManager;
		}

		public DistributedOverlappingMatrix Matrix { get; private set; }

		public void Calculate(DistributedOverlappingIndexer indexer)
		{
			Matrix = new DistributedOverlappingMatrix(environment, indexer, MultiplySubdomainSchurComplement);
		}

		/// <summary>
		/// S[s] * x = (Kbb[s] - Kbi[s] * inv(Kii[s]) * Kib[s]) * x
		/// </summary>
		/// <param name="subdomainID">The ID of a subdomain</param>
		/// <param name="input">The displacements that correspond to boundary dofs of this subdomain.</param>
		/// <param name="output">The forces that correspond to boundary dofs of this subdomain.</param>
		private void MultiplySubdomainSchurComplement(int subdomainID, Vector input, Vector output)
		{
			Vector forces = matrixManager.MultiplyKbb(subdomainID, input);
			Vector temp = matrixManager.MultiplyKib(subdomainID, input);
			temp = matrixManager.MultiplyInverseKii(subdomainID, temp);
			temp = matrixManager.MultiplyKbi(subdomainID, temp);
			forces.SubtractIntoThis(temp);
			output.CopyFrom(forces);
		}
	}
}
