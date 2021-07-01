using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Environments;
using MGroup.Solvers.DomainDecomposition.PSM.Dofs;
using MGroup.Solvers.DomainDecomposition.StiffnessMatrices;
using MGroup.Solvers.DomainDecomposition.PSM.StiffnessMatrices;
using MGroup.LinearAlgebra.Distributed.Overlapping;

namespace MGroup.Solvers.DomainDecomposition.PSM.Vectors
{
	public class PsmSolutionVectorManager : IPsmSolutionVectorManager
	{
		private readonly IComputeEnvironment environment;
		private readonly PsmDofManager dofManager;
		private readonly IDictionary<int, ISubdomainMatrixManager> matrixManagersBasic;
		private readonly IDictionary<int, IPsmSubdomainMatrixManager> matrixManagersPsm;
		private readonly IPsmRhsVectorManager rhsManager;

		public PsmSolutionVectorManager(IComputeEnvironment environment, PsmDofManager dofManager,
			IDictionary<int, ISubdomainMatrixManager> matrixManagersBasic, 
			IDictionary<int, IPsmSubdomainMatrixManager> matrixManagersPsm, IPsmRhsVectorManager rhsManager)
		{
			this.environment = environment;
			this.dofManager = dofManager;
			this.matrixManagersBasic = matrixManagersBasic;
			this.matrixManagersPsm = matrixManagersPsm;
			this.rhsManager = rhsManager;
		}

		public DistributedOverlappingVector InterfaceProblemSolution { get; private set; }

		public void CalcSubdomainDisplacements()
		{
			Action<int> updateSubdomainSolution = subdomainID =>
			{
				Vector uf = CalcUf(subdomainID);
				matrixManagersBasic[subdomainID].SetSolution(uf);
			};
			environment.DoPerNode(updateSubdomainSolution);
		}

		public void Initialize(DistributedOverlappingIndexer indexer)
		{

			InterfaceProblemSolution = new DistributedOverlappingVector(environment, indexer);
		}

		private Vector CalcUf(int subdomainID)
		{
			// Extract internal and boundary parts of rhs vector 
			int numFreeDofs = dofManager.GetSubdomainDofs(subdomainID).NumFreeDofs;
			int[] boundaryDofs = dofManager.GetSubdomainDofs(subdomainID).DofsBoundaryToFree;
			int[] internalDofs = dofManager.GetSubdomainDofs(subdomainID).DofsInternalToFree;

			//TODOMPI: These are no longer necessary with distributed vectors.
			// ub[s] = Lb * ubGlob
			//IMappingMatrix Lb = dofSeparator.GetDofMappingBoundaryClusterToSubdomain(subdomainID);
			//Vector ub = Lb.Multiply(GlobalBoundaryDisplacements, false);

			// ui[s] = inv(Kii[s]) * (fi[s] - Kib[s] * ub[s])
			Vector ub = InterfaceProblemSolution.LocalVectors[subdomainID];
			Vector temp = matrixManagersPsm[subdomainID].MultiplyKib(ub);
			Vector fi = rhsManager.GetInternalRhs(subdomainID);
			temp.LinearCombinationIntoThis(-1.0, fi, +1);
			Vector ui = matrixManagersPsm[subdomainID].MultiplyInverseKii(temp);

			// Gather ub[s], ui[s] into uf[s]
			var uf = Vector.CreateZero(numFreeDofs);
			uf.CopyNonContiguouslyFrom(boundaryDofs, ub);
			uf.CopyNonContiguouslyFrom(internalDofs, ui);

			return uf;
		}
	}
}
