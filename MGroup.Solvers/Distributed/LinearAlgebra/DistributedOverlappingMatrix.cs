using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Exceptions;
using ISAAR.MSolve.LinearAlgebra.Iterative;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.Distributed.Environments;
using MGroup.Solvers.Distributed.Topologies;

//TODOMPI: Do I need a similar class for preconsitioners? Do preconditioners output rhs vectors as well?
namespace MGroup.Solvers.Distributed.LinearAlgebra
{
    public class DistributedOverlappingMatrix : IIterativeMethodMatrix
    {
        private readonly IComputeEnvironment environment;
        private readonly Dictionary<ComputeNode, DistributedIndexer> indexers;         
        private readonly Dictionary<ComputeNode, ILinearTransformation> localMatrices;

        public DistributedOverlappingMatrix(IComputeEnvironment environment,
            Dictionary<ComputeNode, DistributedIndexer> indexers, Dictionary<ComputeNode, ILinearTransformation> localMatrices)
        {
            this.environment = environment;
            this.indexers = indexers;
            this.localMatrices = localMatrices;
        }

        public void MultiplyIntoResult(IIterativeMethodVector lhsVector, IIterativeMethodVector rhsVector)
        {
            if ((lhsVector is DistributedOverlappingVector lhsCasted) && (rhsVector is DistributedOverlappingVector rhsCasted))
            {
                MultiplyIntoResult(lhsCasted, rhsCasted);
            }
            else
            {
                throw new SparsityPatternModifiedException(
                    "This operation is legal only if the lhs and rhs vectors are distributed" +
                    " and have the same indexer as this matrix.");
            }
        }

        public void MultiplyIntoResult(DistributedOverlappingVector lhsVector, DistributedOverlappingVector rhsVector)
        {
            //TODOMPI: check that environment and indexers are the same between A,x and A,y
            Action<ComputeNode> multiplyLocal = node =>
            {
                ILinearTransformation localA = localMatrices[node];
                Vector localX = lhsVector.LocalVectors[node];
                Vector localY = rhsVector.LocalVectors[node];
                localA.Multiply(localX, localY);
            };
            environment.DoPerNode(multiplyLocal);

            rhsVector.SumOverlappingEntries(); 
        }
    }
}
