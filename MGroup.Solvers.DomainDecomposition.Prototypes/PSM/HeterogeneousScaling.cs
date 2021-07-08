using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.PSM
{
    public class HeterogeneousScaling : IPrimalScaling
    {
		private readonly IStructuralModel model;

		public HeterogeneousScaling(IStructuralModel model)
		{
			this.model = model;
		}

		public Dictionary<int, SparseVector> DistributeNodalLoads(Table<INode, IDofType, double> nodalLoads)
        {
			throw new NotImplementedException();
		}
    }
}
