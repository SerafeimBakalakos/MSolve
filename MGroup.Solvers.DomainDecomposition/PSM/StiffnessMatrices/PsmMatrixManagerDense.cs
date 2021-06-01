using System.Collections.Concurrent;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.SchurComplements;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.DomainDecomposition.Commons;
using MGroup.Solvers.DomainDecomposition.PSM.Dofs;
using MGroup.Solvers.DomainDecomposition.StiffnessMatrices;

namespace MGroup.Solvers.DomainDecomposition.PSM.StiffnessMatrices
{
	public class PsmMatrixManagerDense : IPsmMatrixManager
	{
		private readonly IPsmDofSeparator dofSeparator;
		private readonly MatrixManagerDense managerBasic;

		private ConcurrentDictionary<int, Matrix> Kbb = new ConcurrentDictionary<int, Matrix>();
		private ConcurrentDictionary<int, Matrix> Kbi = new ConcurrentDictionary<int, Matrix>();
		private ConcurrentDictionary<int, Matrix> Kib = new ConcurrentDictionary<int, Matrix>();
		private ConcurrentDictionary<int, Matrix> Kii = new ConcurrentDictionary<int, Matrix>();
		private ConcurrentDictionary<int, Matrix> invKii = new ConcurrentDictionary<int, Matrix>();

		public PsmMatrixManagerDense(IPsmDofSeparator dofSeparator, MatrixManagerDense managerBasic)
		{
			this.dofSeparator = dofSeparator;
			this.managerBasic = managerBasic;
		}

		public IMatrixView CalcSchurComplement(int subdomainID)
			=> Kbb[subdomainID] - Kbi[subdomainID] * (invKii[subdomainID] * Kib[subdomainID]);

		public void ClearSubMatrices(int subdomainID)
		{
			Kbb[subdomainID] = null;
			Kbi[subdomainID] = null;
			Kib[subdomainID] = null;
			Kii[subdomainID] = null;
			invKii[subdomainID] = null;
		}

		public void ExtractKiiKbbKib(int subdomainID)
		{
			Matrix Kff = managerBasic.GetMatrixKff(subdomainID);
			int[] boundaryDofs = dofSeparator.GetSubdomainDofsBoundaryToFree(subdomainID);
			int[] internalDofs = dofSeparator.GetSubdomainDofsInternalToFree(subdomainID);
			Kbb[subdomainID] = Kff.GetSubmatrix(boundaryDofs, boundaryDofs);
			Kbi[subdomainID] = Kff.GetSubmatrix(boundaryDofs, internalDofs);
			Kib[subdomainID] = Kff.GetSubmatrix(internalDofs, boundaryDofs);
			Kii[subdomainID] = Kff.GetSubmatrix(internalDofs, internalDofs);
		}

		public void InvertKii(int subdomainID)
		{
			Matrix inverse = Kii[subdomainID].Invert();
			invKii[subdomainID] = inverse;
			Kii[subdomainID] = null;
		}

		public Vector MultiplyInverseKii(int subdomainID, Vector vector) => invKii[subdomainID] * vector;

		public Vector MultiplyKbb(int subdomainID, Vector vector) => Kbb[subdomainID] * vector;

		public Vector MultiplyKbi(int subdomainID, Vector vector) => Kbi[subdomainID] * vector;

		public Vector MultiplyKib(int subdomainID, Vector vector) => Kib[subdomainID] * vector;

		public void ReorderInternalDofs(int subdomainID)
		{
			dofSeparator.ReorderSubdomainInternalDofs(subdomainID, DofPermutation.CreateNoPermutation());
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
