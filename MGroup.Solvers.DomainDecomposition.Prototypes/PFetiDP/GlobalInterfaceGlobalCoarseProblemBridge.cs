using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using MGroup.Solvers.DomainDecomposition.Prototypes.FetiDP;
using MGroup.Solvers.DomainDecomposition.Prototypes.PSM;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.PFetiDP
{
	public class GlobalInterfaceGlobalCoarseProblemBridge
	{
		private readonly PsmInterfaceProblemGlobal interfaceProblem;
		private readonly FetiDPCoarseProblemGlobal coarseProblem;

		public GlobalInterfaceGlobalCoarseProblemBridge(PsmInterfaceProblemGlobal interfaceProblem, 
			FetiDPCoarseProblemGlobal coarseProblem)
		{
			this.interfaceProblem = interfaceProblem;
			this.coarseProblem = coarseProblem;
		}

		public Matrix GlobalMatrixNcb { get; set; }

		public IStructuralModel Model { get; set; }

		public void LinkInterfaceCoarseProblems()
		{
			DofTable boundaryDofs = interfaceProblem.GlobalDofOrderingBoundary;
			DofTable cornerDofs = coarseProblem.GlobalDofOrderingCorner;
			GlobalMatrixNcb = Matrix.CreateZero(coarseProblem.NumGlobalDofsCorner, interfaceProblem.NumGlobalDofsBoundary);
			foreach ((INode node, IDofType dof, int c) in cornerDofs)
			{
				int b = boundaryDofs[node, dof];
				GlobalMatrixNcb[c, b] = 1.0;
			}
		}
	}
}
