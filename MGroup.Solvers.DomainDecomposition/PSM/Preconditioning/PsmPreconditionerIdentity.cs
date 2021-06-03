using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Environments;
using MGroup.LinearAlgebra.Distributed.IterativeMethods.Preconditioning;
using MGroup.LinearAlgebra.Distributed.Overlapping;
using MGroup.Solvers.DomainDecomposition.PSM.InterfaceProblem;

namespace MGroup.Solvers.DomainDecomposition.PSM.Preconditioning
{
	public class PsmPreconditionerIdentity : IPsmPreconditioner
	{

		public PsmPreconditionerIdentity()
		{
		}

		public IPreconditioner Preconditioner { get; private set; }

		public void Calculate(IComputeEnvironment environment, DistributedOverlappingIndexer indexer,
			IPsmInterfaceProblemMatrix interfaceProblemMatrix) 
		{
			Preconditioner = new IdentityPreconditioner();
		}
	}
}
