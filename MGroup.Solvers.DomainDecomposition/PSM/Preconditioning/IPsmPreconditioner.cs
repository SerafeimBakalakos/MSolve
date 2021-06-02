using System;
using System.Collections.Generic;
using System.Text;
using MGroup.LinearAlgebra.Distributed.IterativeMethods.Preconditioning;
using MGroup.LinearAlgebra.Distributed.Overlapping;

namespace MGroup.Solvers.DomainDecomposition.PSM.Preconditioning
{
	public interface IPsmPreconditioner
	{
		void Calculate(DistributedOverlappingIndexer indexer);

		IPreconditioner Preconditioner { get; }
	}
}
