using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.LinearAlgebra.Distributed.IterativeMethods.Preconditioning;
using MGroup.LinearAlgebra.Distributed.Overlapping;

namespace MGroup.Solvers.DomainDecomposition.PSM.Preconditioning
{
	public class PsmPreconditionerIdentity : IPsmPreconditioner
	{

		public PsmPreconditionerIdentity()
		{
		}

		public IPreconditioner Preconditioner { get; private set; }

		public void Calculate(DistributedOverlappingIndexer indexer) 
		{
			Preconditioner = new IdentityPreconditioner();
		}
	}
}
