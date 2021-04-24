using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Iterative;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Solvers.MPI.Environments;
using MGroup.Solvers.MPI.Topologies;

//TODOMPI: Do I need a similar class for preconsitioners? Do preconditioners output rhs vectors as well?
namespace MGroup.Solvers.MPI.LinearAlgebra
{
    public class DistributedOverlappingMatrix
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

        public void MultiplyVector(DistributedOverlappingVector x, DistributedOverlappingVector y)
        {
            //TODOMPI: check that environment and indexers are the same between A,x and A,y
            Action<ComputeNode> multiplyLocal = node =>
            {
                ILinearTransformation localA = localMatrices[node];
                Vector localX = x.LocalVectors[node];
                Vector localY = y.LocalVectors[node];
                localA.Multiply(localX, localY);
            };
            environment.DoPerNode(multiplyLocal);

            // Sum the values of corresponding boundary entries across local vectors. 
            y.ConvertRhsToLhsVector(); 
        }
    }
}
