using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ISAAR.MSolve.Discretization.Interfaces;
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
		private readonly IPsmDofSeparator dofSeparator;
		private readonly IPsmMatrixManager matrixManager;

		private readonly ConcurrentDictionary<int, Vector> vectorsFi = new ConcurrentDictionary<int, Vector>();

		public PsmRhsVectorManager(IComputeEnvironment environment, IPsmDofSeparator dofSeparator,
			Dictionary<int, ILinearSystem> linearSystems, IPsmMatrixManager matrixManager)
		{
			this.environment = environment;
			this.dofSeparator = dofSeparator;
			this.linearSystems = linearSystems;
			this.matrixManager = matrixManager;
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
			int[] boundaryDofs = dofSeparator.GetSubdomainDofsBoundaryToFree(subdomainID);
			Vector ff = (Vector)linearSystems[subdomainID].RhsVector;
			Vector fb = ff.GetSubvector(boundaryDofs);

			// Static condensation: fbCondensed[s] = fb[s] - Kbi[s] * inv(Kii[s]) * fi[s]
			Vector fi = vectorsFi[subdomainID];
			Vector temp = matrixManager.MultiplyInverseKii(subdomainID, fi);
			temp = matrixManager.MultiplyKbi(subdomainID, temp);
			fb.SubtractIntoThis(temp);

			return fb;
		}

		private void ExtractInternalRhsVector(int subdomainID)
		{
			int[] internalDofs = dofSeparator.GetSubdomainDofsInternalToFree(subdomainID);
			Vector ff = (Vector)linearSystems[subdomainID].RhsVector;
			Vector fi = ff.GetSubvector(internalDofs);
			vectorsFi[subdomainID] = fi;
        }
    }
}
