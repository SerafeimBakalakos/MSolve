using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.DomainDecomposition.Prototypes.LinearAlgebraExtensions;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.FetiDP
{
	public class FetiDPCoarseProblemGlobal
	{
		private Matrix invScc;

		public FetiDPSubdomainDofs Dofs { get; set; }

		public DofTable GlobalDofOrderingCorner { get; set; }

		public BlockMatrix MatrixLce { get; set; }

		public IStructuralModel Model { get; set; }

		public int NumGlobalDofsCorner { get; set; }

		public FetiDPSubdomainStiffnesses Stiffnesses { get; set; }

		public Dictionary<int, Matrix> SubdomainMatricesLc { get; } = new Dictionary<int, Matrix>();

		public void FindDofs()
		{
			FindGlobalCornerDofs();
			MapCornerDofsGlobalToSubdomains();
			CalcLce();
		}

		public void InitializeMatrix()
		{
			var globalScc = Matrix.CreateZero(NumGlobalDofsCorner, NumGlobalDofsCorner);
			foreach (ISubdomain subdomain in Model.Subdomains)
			{
				int s = subdomain.ID;
				Matrix Lc = SubdomainMatricesLc[s];
				Matrix localScc = Stiffnesses.Scc[s];
				globalScc.AddIntoThis(Lc.Transpose() * localScc * Lc);
			}
			this.invScc = globalScc.Invert();
		}

		public Vector Solve(Vector rhsVector) => invScc * rhsVector;

		private void CalcLce()
		{
			MatrixLce = BlockMatrix.CreateCol(Dofs.NumSubdomainDofsCorner, NumGlobalDofsCorner);
			foreach (ISubdomain subdomain in Model.Subdomains)
			{
				int s = subdomain.ID;
				MatrixLce.AddBlock(s, 0, SubdomainMatricesLc[s]);
			}
		}

		private void FindGlobalCornerDofs()
		{
			var globalCornerDofs = new SortedDofTable();
			int numCornerDofs = 0;
			foreach (ISubdomain subdomain in Model.Subdomains)
			{
				foreach ((INode node, IDofType dof, int idx) in Dofs.SubdomainDofOrderingCorner[subdomain.ID])
				{
					bool didNotExist = globalCornerDofs.TryAdd(node.ID, AllDofs.GetIdOfDof(dof), numCornerDofs);
					if (didNotExist)
					{
						numCornerDofs++;
					}
				}
			}

			var cornerDofOrdering = new DofTable();
			foreach ((int nodeID, int dofID, int idx) in globalCornerDofs)
			{
				cornerDofOrdering[Model.GetNode(nodeID), AllDofs.GetDofWithId(dofID)] = idx;
			}

			GlobalDofOrderingCorner = cornerDofOrdering;
			NumGlobalDofsCorner = numCornerDofs;
		}

		private void MapCornerDofsGlobalToSubdomains()
		{
			foreach (ISubdomain subdomain in Model.Subdomains)
			{
				DofTable subdomainDofs = Dofs.SubdomainDofOrderingCorner[subdomain.ID];
				var Lc = Matrix.CreateZero(Dofs.NumSubdomainDofsCorner[subdomain.ID], NumGlobalDofsCorner);
				foreach ((INode node, IDofType dof, int subdomainIdx) in subdomainDofs)
				{
					int globalIdx = GlobalDofOrderingCorner[node, dof];
					Lc[subdomainIdx, globalIdx] = 1.0;
				}
				SubdomainMatricesLc[subdomain.ID] = Lc;
			}
		}
	}
}
