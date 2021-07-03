using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.DomainDecomposition.PSM.Dofs;
using MGroup.Solvers.DomainDecomposition.PSM.StiffnessMatrices;
using MGroup.Solvers.DomainDecomposition.StiffnessMatrices;
using MGroup.Solvers.LinearSystems;

namespace MGroup.Solvers.DomainDecomposition.PSM.Vectors
{
    public class PsmSubdomainVectors
    {
		private readonly ILinearSystem linearSystem;
		private readonly ISubdomainMatrixManager matrixManagerBasic;
		private readonly IPsmSubdomainMatrixManager matrixManagerPsm;
		private readonly PsmSubdomainDofs subdomainDofs;

		private Vector vectorFi;

		public PsmSubdomainVectors(PsmSubdomainDofs subdomainDofs, ILinearSystem linearSystem,
			ISubdomainMatrixManager matrixManagerBasic, IPsmSubdomainMatrixManager matrixManagerPsm)
		{
			this.subdomainDofs = subdomainDofs;
			this.matrixManagerBasic = matrixManagerBasic;
			this.matrixManagerPsm = matrixManagerPsm;
			this.linearSystem = linearSystem;
		}

		public Vector CalcCondensedRhsVector()
		{
			// Extract boundary part of rhs vector 
			int[] boundaryDofs = subdomainDofs.DofsBoundaryToFree;
			Vector ff = (Vector)linearSystem.RhsVector;
			Vector fb = ff.GetSubvector(boundaryDofs);

			// Static condensation: fbCondensed[s] = fb[s] - Kbi[s] * inv(Kii[s]) * fi[s]
			Vector temp = matrixManagerPsm.MultiplyInverseKii(vectorFi);
			temp = matrixManagerPsm.MultiplyKbi(temp);
			fb.SubtractIntoThis(temp);

			return fb;
		}

		public void Clear()
		{
			vectorFi = null;
		}

		public void ExtractInternalRhsVector()
		{
			int[] internalDofs = subdomainDofs.DofsInternalToFree;
			Vector ff = (Vector)linearSystem.RhsVector;
			this.vectorFi = ff.GetSubvector(internalDofs);
		}

		public void CalcSubdomainFreeSolution(Vector subdomainBoundarySolution)
		{
			// Extract internal and boundary parts of rhs vector 
			int numFreeDofs = subdomainDofs.NumFreeDofs;
			int[] boundaryDofs = subdomainDofs.DofsBoundaryToFree;
			int[] internalDofs = subdomainDofs.DofsInternalToFree;

			// ui[s] = inv(Kii[s]) * (fi[s] - Kib[s] * ub[s])
			Vector ub = subdomainBoundarySolution;
			Vector temp = matrixManagerPsm.MultiplyKib(ub);
			temp.LinearCombinationIntoThis(-1.0, vectorFi, +1);
			Vector ui = matrixManagerPsm.MultiplyInverseKii(temp);

			// Gather ub[s], ui[s] into uf[s]
			var uf = Vector.CreateZero(numFreeDofs);
			uf.CopyNonContiguouslyFrom(boundaryDofs, subdomainBoundarySolution);
			uf.CopyNonContiguouslyFrom(internalDofs, ui);

			matrixManagerBasic.SetSolution(uf);
		}
	}
}
