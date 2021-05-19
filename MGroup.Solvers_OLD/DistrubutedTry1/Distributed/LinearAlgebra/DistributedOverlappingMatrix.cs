using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Exceptions;
using ISAAR.MSolve.LinearAlgebra.Iterative;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.Environments;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.Topologies;

//TODOMPI: Do I need a similar class for preconsitioners? Do preconditioners output rhs vectors as well?
namespace MGroup.Solvers_OLD.DistributedTry1.Distributed.LinearAlgebra
{
    public class DistributedOverlappingMatrix : IIterativeMethodMatrix
    {
        private readonly IComputeEnvironment environment;
        private readonly DistributedIndexer indexer;
        //private readonly Dictionary<ComputeNode, DistributedIndexer> indexers;         
        private readonly Dictionary<ComputeNode, ILinearTransformation> localMatrices;

        public DistributedOverlappingMatrix(IComputeEnvironment environment, DistributedIndexer indexer, 
            Dictionary<ComputeNode, ILinearTransformation> localMatrices)
        {
            this.environment = environment;
            this.indexer = indexer;
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
            //Debug.Assert((this.environment == lhsVector.environment) && (this.indexer == lhsVector.indexer));
            //Debug.Assert((this.environment == rhsVector.environment) && (this.indexer == rhsVector.indexer));

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
