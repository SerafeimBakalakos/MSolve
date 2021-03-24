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
using MGroup.Solvers.DDM.StiffnessMatrices;

namespace MGroup.Solvers.DDM.Psm.Vectors
{
	public class PsmSolutionVectorManager : IPsmSolutionVectorManager
	{
		private const int clusterID = 0;
		private readonly Dictionary<int, ILinearSystem> linearSystems;
		private readonly IProcessingEnvironment environment;
		private readonly IStructuralModel model;
		private readonly IPsmDofSeparator dofSeparator;
		private readonly IMatrixManager matrixManagerBasic;
		private readonly IPsmMatrixManager matrixManagerPsm;
		private readonly IPsmRhsVectorManager rhsManager;

		public PsmSolutionVectorManager(IProcessingEnvironment environment, IStructuralModel model, 
			Dictionary<int, ILinearSystem> linearSystems, IPsmDofSeparator dofSeparator,
			IMatrixManager matrixManagerBasic, IPsmMatrixManager matrixManagerPsm, IPsmRhsVectorManager rhsManager)
		{
			this.environment = environment;
			this.model = model;
			this.linearSystems = linearSystems;
			this.dofSeparator = dofSeparator;
			this.matrixManagerBasic = matrixManagerBasic;
			this.matrixManagerPsm = matrixManagerPsm;
			this.rhsManager = rhsManager;
		}

		public Vector GlobalBoundaryDisplacements { get; private set; }

		public void CalcSubdomainDisplacements()
		{
			Action<ISubdomain> calcUf = sub =>
			{
				Vector uf = CalcUf(sub.ID);
				matrixManagerBasic.SetSolution(sub.ID, uf);
			};
			environment.ExecuteSubdomainAction(model.Subdomains, calcUf);
		}

		public void Initialize()
		{
			GlobalBoundaryDisplacements = Vector.CreateZero(dofSeparator.GetClusterNumBoundaryDofs(clusterID));
		}

		private Vector CalcUf(int subdomainID)
		{
			// Extract internal and boundary parts of rhs vector 
			int numFreeDofs = dofSeparator.GetNumFreeDofs(subdomainID);
			int[] boundaryDofs = dofSeparator.GetDofsBoundaryToFree(subdomainID);
			int[] internalDofs = dofSeparator.GetDofsInternalToFree(subdomainID);
			IMappingMatrix Lb = dofSeparator.GetDofMappingBoundaryClusterToSubdomain(subdomainID);

			// ub[s] = Lb * ubGlob
			// ui[s] = inv(Kii[s]) * (fi[s] - Kib[s] * ub[s])
			Vector ub = Lb.Multiply(GlobalBoundaryDisplacements, false);
			Vector temp = matrixManagerPsm.MultiplyKib(subdomainID, ub);
			Vector fi = rhsManager.GetInternalRhs(subdomainID);
			temp.LinearCombinationIntoThis(-1.0, fi, +1);
			Vector ui = matrixManagerPsm.MultiplyInverseKii(subdomainID, temp);

			// Gather ub[s], ui[s] into uf[s]
			var uf = Vector.CreateZero(numFreeDofs);
			uf.CopyNonContiguouslyFrom(boundaryDofs, ub);
			uf.CopyNonContiguouslyFrom(internalDofs, ui);

			return uf;
		}
	}
}
