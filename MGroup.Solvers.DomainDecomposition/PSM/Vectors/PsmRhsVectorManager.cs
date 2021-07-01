using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.LinearSystems;
using MGroup.Environments;
using MGroup.LinearAlgebra.Distributed.Overlapping;
using MGroup.Solvers.DomainDecomposition.PSM.Dofs;
using MGroup.Solvers.DomainDecomposition.PSM.StiffnessMatrices;

namespace MGroup.Solvers.DomainDecomposition.PSM.Vectors
{
	public class PsmRhsVectorManager : IPsmRhsVectorManager
	{
		private readonly IComputeEnvironment environment;
		private readonly Dictionary<int, ILinearSystem> linearSystems;
		private readonly PsmDofManager dofManager;
		private readonly IDictionary<int, IPsmSubdomainMatrixManager> matrixManagers;

		private readonly ConcurrentDictionary<int, Vector> vectorsFi = new ConcurrentDictionary<int, Vector>();

		public PsmRhsVectorManager(IComputeEnvironment environment, PsmDofManager dofManager,
			Dictionary<int, ILinearSystem> linearSystems, IDictionary<int, IPsmSubdomainMatrixManager> matrixManagers)
		{
			this.environment = environment;
			this.dofManager = dofManager;
			this.linearSystems = linearSystems;
			this.matrixManagers = matrixManagers;
		}

		public DistributedOverlappingVector InterfaceProblemRhs { get; private set; }

		// globalF = sum {Lb[s]^T * (fb[s] - Kbi[s] * inv(Kii[s]) * fi[s]) }
		public void CalcRhsVectors(DistributedOverlappingIndexer indexer)
		{
			environment.DoPerNode(ExtractInternalRhsVector);
			Dictionary<int, Vector> fbCondensed = environment.CreateDictionaryPerNode(CalcCondensedRhsVector);
			InterfaceProblemRhs = new DistributedOverlappingVector(environment, indexer, fbCondensed);
			InterfaceProblemRhs.SumOverlappingEntries();
		}

		public void Clear()
		{
			vectorsFi.Clear();
			InterfaceProblemRhs = null;
		}

		public Vector GetInternalRhs(int subdomainID) => vectorsFi[subdomainID];

		// Use reflection to test this method.
		private Vector CalcCondensedRhsVector(int subdomainID)
		{
			// Extract boundary part of rhs vector 
			int[] boundaryDofs = dofManager.GetSubdomainDofs(subdomainID).DofsBoundaryToFree;
			Vector ff = (Vector)linearSystems[subdomainID].RhsVector;
			Vector fb = ff.GetSubvector(boundaryDofs);

			// Static condensation: fbCondensed[s] = fb[s] - Kbi[s] * inv(Kii[s]) * fi[s]
			Vector fi = vectorsFi[subdomainID];
			Vector temp = matrixManagers[subdomainID].MultiplyInverseKii(fi);
			temp = matrixManagers[subdomainID].MultiplyKbi(temp);
			fb.SubtractIntoThis(temp);

			return fb;
		}

		private void ExtractInternalRhsVector(int subdomainID)
		{
			int[] internalDofs = dofManager.GetSubdomainDofs(subdomainID).DofsInternalToFree;
			Vector ff = (Vector)linearSystems[subdomainID].RhsVector;
			Vector fi = ff.GetSubvector(internalDofs);
			vectorsFi[subdomainID] = fi;
        }
    }
}
