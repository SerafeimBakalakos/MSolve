using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Environments;
using MGroup.LinearAlgebra.Distributed;
using MGroup.LinearAlgebra.Distributed.IterativeMethods.Preconditioning;
using MGroup.LinearAlgebra.Distributed.Overlapping;
using MGroup.Solvers.DomainDecomposition.PSM.InterfaceProblem;
using MGroup.Solvers.DomainDecomposition.PSM.StiffnessMatrices;

namespace MGroup.Solvers.DomainDecomposition.PSM.Preconditioning
{
    public class PsmPreconditionerJacobi : IPsmPreconditioner
    {
        public PsmPreconditionerJacobi()
        {
        }

        public IPreconditioner Preconditioner { get; private set; }

        public void Calculate(IComputeEnvironment environment, DistributedOverlappingIndexer indexer,
            IPsmInterfaceProblemMatrix interfaceProblemMatrix)
        {
            Func<int, Vector> extractDiagonal = subdomainID 
                => Vector.CreateFromArray(interfaceProblemMatrix.ExtractDiagonal(subdomainID));
            Dictionary<int, Vector> localDiagonals = environment.CreateDictionaryPerNode(extractDiagonal);
            var distributedDiagonal = new DistributedOverlappingVector(environment, indexer, localDiagonals);
            
            // All dofs belong to 2 or more subdomains and must have the stiffness contributions from all these subdomains.
            distributedDiagonal.SumOverlappingEntries();

            this.Preconditioner = new DistributedOverlappingJacobiPreconditioner(environment, distributedDiagonal);
        }
    }
}
