﻿using System;
using System.Collections.Generic;
using System.Text;
using MGroup.Environments;
using MGroup.LinearAlgebra.Distributed.IterativeMethods.Preconditioning;
using MGroup.LinearAlgebra.Distributed.Overlapping;
using MGroup.Solvers.DomainDecomposition.PSM.InterfaceProblem;

namespace MGroup.Solvers.DomainDecomposition.PSM.Preconditioning
{
	public interface IPsmPreconditioner
	{
		void Calculate(IComputeEnvironment environment, DistributedOverlappingIndexer indexer, 
			IPsmInterfaceProblemMatrix interfaceProblemMatrix);

		IPreconditioner Preconditioner { get; }
	}
}