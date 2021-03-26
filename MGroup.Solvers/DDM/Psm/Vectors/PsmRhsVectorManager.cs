using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Solvers.LinearSystems;
using MGroup.Solvers.DDM.Environments;
using MGroup.Solvers.DDM.Mappings;
using MGroup.Solvers.DDM.Psm.Dofs;
using MGroup.Solvers.DDM.Psm.StiffnessMatrices;

namespace MGroup.Solvers.DDM.Psm.Vectors
{
	public class PsmRhsVectorManager : IPsmRhsVectorManager
	{
		private const int clusterID = 0;
		private readonly IProcessingEnvironment environment;
		private readonly IStructuralModel model;
		private readonly Dictionary<int, ILinearSystem> linearSystems;
		private readonly IPsmDofSeparator dofSeparator;
		private readonly IPsmMatrixManager matrixManager;

		private readonly Dictionary<int, Vector> vectorsFbCondensed;
		private readonly Dictionary<int, Vector> vectorsFi;

		public PsmRhsVectorManager(IProcessingEnvironment environment, IStructuralModel model, Dictionary<int, ILinearSystem> linearSystems, 
			IPsmDofSeparator dofSeparator, IPsmMatrixManager matrixManager)
		{
			this.environment = environment;
			this.model = model;
			this.linearSystems = linearSystems;
			this.dofSeparator = dofSeparator;
			this.matrixManager = matrixManager;

			this.vectorsFi = new Dictionary<int, Vector>();
			this.vectorsFbCondensed = new Dictionary<int, Vector>();
		}

		public Vector InterfaceProblemRhs { get; private set; }

		public void CalcRhsVectors()
		{
			// globalF = sum {Lb[s]^T * (fb[s] - Kbi[s] * inv(Kii[s]) * fi[s]) }
			var partialRhsVectors = new List<Vector>(linearSystems.Count);
			Action<ISubdomain> calcSubdomainRhs = sub =>
			{
				int s = sub.ID;
				CalcSubdomainRhs(s);
				Vector subdomainRhs = vectorsFbCondensed[s];
				IMappingMatrix Lb = dofSeparator.GetDofMappingBoundaryClusterToSubdomain(s);
				Vector partialRhs = Lb.Multiply(subdomainRhs, true);
				lock (partialRhsVectors) partialRhsVectors.Add(partialRhs);
			};
			environment.ExecuteSubdomainAction(model.Subdomains, calcSubdomainRhs);

			int numGlobalBoundaryDofs = dofSeparator.GetNumBoundaryDofsCluster(clusterID);
			var globalBoundaryRhs = Vector.CreateZero(numGlobalBoundaryDofs);
			environment.ReduceAddVectors(partialRhsVectors, globalBoundaryRhs);
			InterfaceProblemRhs = globalBoundaryRhs;
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

			lock (vectorsFi) vectorsFi[subdomainID] = fi;
			lock (vectorsFbCondensed) vectorsFbCondensed[subdomainID] = fb;
		}
	}
}
