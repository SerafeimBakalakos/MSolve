using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Solvers.LinearSystems;
using MGroup.Solvers_OLD.DDM.Mappings;
using MGroup.Solvers_OLD.DDM.Psm.StiffnessMatrices;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.Environments;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.LinearAlgebra;
using System.Collections.Concurrent;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.Topologies;
using MGroup.Solvers_OLD.DistributedTry1.DDM.Psm.Dofs;

namespace MGroup.Solvers_OLD.DistributedTry1.DDM.Psm.Vectors
{
	public class PsmRhsVectorManager_NEW : IPsmRhsVectorManager_NEW
	{
		private readonly IComputeEnvironment environment;
		private readonly IStructuralModel model;
		private readonly ClusterTopology clusterTopology;
		private readonly Dictionary<int, ILinearSystem> linearSystems;
		private readonly IPsmDofSeparator_NEW dofSeparator;
		private readonly IPsmMatrixManager matrixManager;

		private readonly ConcurrentDictionary<int, Vector> vectorsFbCondensed = new ConcurrentDictionary<int, Vector>();
		private readonly ConcurrentDictionary<int, Vector> vectorsFi = new ConcurrentDictionary<int, Vector>();

		public PsmRhsVectorManager_NEW(IComputeEnvironment environment, IStructuralModel model, ClusterTopology clusterTopology,
			Dictionary<int, ILinearSystem> linearSystems, IPsmDofSeparator_NEW dofSeparator, IPsmMatrixManager matrixManager)
		{
			this.environment = environment;
			this.model = model;
			this.clusterTopology = clusterTopology;
			this.linearSystems = linearSystems;
			this.dofSeparator = dofSeparator;
			this.matrixManager = matrixManager;
		}

		public DistributedOverlappingVector InterfaceProblemRhs { get; private set; }

		// globalF = sum {Lb[s]^T * (fb[s] - Kbi[s] * inv(Kii[s]) * fi[s]) }
		public void CalcRhsVectors(DistributedIndexer indexer)
		{
			//TODOMPI: Ideally this should be done at the same time as communication between subdomains (of the same cluster at the very least)
			environment.DoPerSubnode(computeSubnode => CalcSubdomainRhs(computeSubnode.ID));
			InterfaceProblemRhs = AssembleGlobalVectorFromSubdomainVectors(
				environment, indexer, dofSeparator, vectorsFbCondensed);
			InterfaceProblemRhs.SumOverlappingEntries();
		}

		//TODOMPI: Option to delay computation of subdomain vectors or pipeline it with the assembly.
		//TODOMPI: Option to write over an existing vector (ask that it is cleared)
		private static DistributedOverlappingVector AssembleGlobalVectorFromSubdomainVectors(IComputeEnvironment environment, 
			DistributedIndexer indexer, IPsmDofSeparator_NEW dofSeparator, IReadOnlyDictionary<int, Vector> subdomainVectors) 
		{
			var globalVector = new DistributedOverlappingVector(environment, indexer);
			Action<ComputeNode> clusterAction = computeNode =>
			{
				Vector clusterVector = globalVector.LocalVectors[computeNode];
				foreach (ComputeSubnode computeSubnode in computeNode.Subnodes.Values)
				{
					BooleanMatrixRowsToColumns Lb = environment.AccessSubnodeDataFromNode(computeSubnode,
						subnode => dofSeparator.GetDofMappingBoundaryClusterToSubdomain(subnode.ID));
					Vector subdomainVector = environment.AccessSubnodeDataFromNode(computeSubnode,
						subnode => subdomainVectors[subnode.ID]);

					//TODOMPI: this should be done by IMappingMatrix to take scaling into account.
					clusterVector.AddIntoThisNonContiguouslyFrom(Lb.RowsToColumns, subdomainVector);
				}
			};
			environment.DoPerNode(clusterAction);
			return globalVector;
		}

		public void Clear()
		{
			vectorsFi.Clear();
			vectorsFbCondensed.Clear();
			InterfaceProblemRhs = null;
		}

		public Vector GetBoundaryCondensedRhs(int subdomainID) => vectorsFbCondensed[subdomainID];

		public Vector GetInternalRhs(int subdomainID) => vectorsFi[subdomainID];

		private void CalcSubdomainRhs(int subdomainID)
		{
			// Extract internal and boundary parts of rhs vector 
			int[] boundaryDofs = dofSeparator.GetSubdomainDofsBoundaryToFree(subdomainID);
			int[] internalDofs = dofSeparator.GetSubdomainDofsInternalToFree(subdomainID);
			Vector ff = (Vector)linearSystems[subdomainID].RhsVector;
			Vector fb = ff.GetSubvector(boundaryDofs);
			Vector fi = ff.GetSubvector(internalDofs);

			// Static condensation: fbCondensed[s] = fb[s] - Kbi[s] * inv(Kii[s]) * fi[s]
			Vector temp = matrixManager.MultiplyInverseKii(subdomainID, fi);
			temp = matrixManager.MultiplyKbi(subdomainID, temp);
			fb.SubtractIntoThis(temp);

			vectorsFi[subdomainID] = fi;
			vectorsFbCondensed[subdomainID] = fb;
		}
	}
}
