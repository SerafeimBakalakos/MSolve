using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Environments;
using MGroup.LinearAlgebra.Distributed.Overlapping;
using MGroup.Solvers.DomainDecomposition.PSM.StiffnessMatrices;

namespace MGroup.Solvers.DomainDecomposition.PSM.InterfaceProblem
{
	/// <summary>
	/// There is only 1 explicit matrix per subdomain, which was calculated by operations between multiple intermediate matrices,
	/// all refering to the same subdomain.
	/// </summary>
	public class PsmInterfaceProblemMatrixExplicit : IPsmInterfaceProblemMatrix
	{
		private readonly IComputeEnvironment environment;
		private readonly IPsmMatrixManager matrixManager;
		private readonly ConcurrentDictionary<int, IMatrixView> schurComplementsPerSubdomain 
			= new ConcurrentDictionary<int, IMatrixView>();

		public PsmInterfaceProblemMatrixExplicit(IComputeEnvironment environment, IPsmMatrixManager matrixManager)
		{
			this.environment = environment;
			this.matrixManager = matrixManager;
		}

		public DistributedOverlappingMatrix Matrix { get; private set; }

		public void Calculate(DistributedOverlappingIndexer indexer)
		{
			//Sbb[s] = Kbb[s] - Kbi[s] * inv(Kii[s]) * Kib[s]
			schurComplementsPerSubdomain.Clear();
			Action<int> calcSchurComplement = subdomainID =>
			{
				IMatrixView Sbb = matrixManager.CalcSchurComplement(subdomainID);
				schurComplementsPerSubdomain[subdomainID] = Sbb;
			};
			environment.DoPerNode(calcSchurComplement);

			Matrix = new DistributedOverlappingMatrix(environment, indexer, MultiplySubdomainSchurComplement);
		}

		public double[] ExtractDiagonal(int subdomainID)
		{
			IMatrixView Sbb = schurComplementsPerSubdomain[subdomainID];
			return Sbb.GetDiagonalAsArray(); //TODO: this should be a polymorphic method, the extension can be too slow
		}

		/// <summary>
		/// Sbb[s] * x = (Kbb[s] - Kbi[s] * inv(Kii[s]) * Kib[s]) * x
		/// </summary>
		/// <param name="subdomainID">The ID of a subdomain</param>
		/// <param name="input">The displacements that correspond to boundary dofs of this subdomain.</param>
		/// <param name="output">The forces that correspond to boundary dofs of this subdomain.</param>
		private void MultiplySubdomainSchurComplement(int subdomainID, Vector input, Vector output)
			=> schurComplementsPerSubdomain[subdomainID].MultiplyIntoResult(input, output);
	}
}