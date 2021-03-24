using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Solvers.LinearSystems;
using MGroup.Solvers.Assemblers;
using MGroup.Solvers.DDM.Dofs;
using MGroup.Solvers.DDM.Psm.Dofs;
using MGroup.Solvers.DDM.StiffnessMatrices;
using MGroup.Solvers.DofOrdering.Reordering;
using MGroup.Solvers.LinearSystems;

namespace MGroup.Solvers.DDM.Psm.StiffnessMatrices
{
	public class PsmMatrixManagerDense : IPsmMatrixManager
	{
		private readonly IPsmDofSeparator dofSeparator;
		private readonly MatrixManagerDense managerBasic;

		private Dictionary<int, Matrix> Kbb = new Dictionary<int, Matrix>();
		private Dictionary<int, Matrix> Kib = new Dictionary<int, Matrix>();
		private Dictionary<int, Matrix> Kbi = new Dictionary<int, Matrix>();
		private Dictionary<int, Matrix> Kii = new Dictionary<int, Matrix>();
		private Dictionary<int, Matrix> invKii = new Dictionary<int, Matrix>();

		public PsmMatrixManagerDense(IPsmDofSeparator dofSeparator, MatrixManagerDense managerBasic)
		{
			this.dofSeparator = dofSeparator;
			this.managerBasic = managerBasic;
		}

		public void ClearSubMatrices(int subdomainID)
		{
			lock (Kbb) Kbb[subdomainID] = null;
			lock (Kbi) Kbi[subdomainID] = null;
			lock (Kib) Kib[subdomainID] = null;
			lock (Kii) Kii[subdomainID] = null;
			lock (invKii) invKii[subdomainID] = null;
		}

		public void ExtractKiiKbbKib(int subdomainID)
		{
			Matrix Kff = managerBasic.GetMatrixKff(subdomainID);
			int[] boundaryDofs = dofSeparator.GetDofsBoundaryToFree(subdomainID);
			int[] internalDofs = dofSeparator.GetDofsInternalToFree(subdomainID);
			lock (Kbb) Kbb[subdomainID] = Kff.GetSubmatrix(boundaryDofs, boundaryDofs);
			lock (Kbi) Kbi[subdomainID] = Kff.GetSubmatrix(boundaryDofs, internalDofs);
			lock (Kib) Kib[subdomainID] = Kff.GetSubmatrix(internalDofs, boundaryDofs);
			lock (Kii) Kii[subdomainID] = Kff.GetSubmatrix(internalDofs, internalDofs);
		}

		public void InvertKii(int subdomainID)
		{
			Matrix inverse = Kii[subdomainID].Invert();
			lock (invKii) invKii[subdomainID] = inverse;
			lock (Kii) Kii[subdomainID] = null;
		}

		public Vector MultiplyInverseKii(int subdomainID, Vector vector) => invKii[subdomainID] * vector;

		public Vector MultiplyKbb(int subdomainID, Vector vector) => Kbb[subdomainID] * vector;

		public Vector MultiplyKbi(int subdomainID, Vector vector) => Kbi[subdomainID] * vector;

		public Vector MultiplyKib(int subdomainID, Vector vector) => Kib[subdomainID] * vector;

		public void ReorderInternalDofs(int subdomainID)
		{
			dofSeparator.ReorderInternalDofs(subdomainID, DofPermutation.CreateNoPermutation());
		}

		public class Factory : IPsmMatrixManagerFactory
		{
			public (IMatrixManager, IPsmMatrixManager) CreateMatrixManagers(IStructuralModel model, IPsmDofSeparator dofSeparator)
			{
				var basicMatrixManager = new MatrixManagerDense(model);
				var psmMatrixManager = new PsmMatrixManagerDense(dofSeparator, basicMatrixManager);
				return (basicMatrixManager, psmMatrixManager);
			}
		}
	}
}
