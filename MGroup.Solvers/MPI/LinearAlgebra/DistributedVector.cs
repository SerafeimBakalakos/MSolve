using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;

//TODO: have an indexer object that determines overlaps and perhaps whether it is a lhs or rhs vector (although we would want
//      to use the same indexer for all vectors and matrices of the same distributed linear system. Use the indexer to check if 
//      linear algebra operations are done between vertices & matrices of the same indexer. Could the linear system be this 
//      indexer?
namespace MGroup.Solvers.MPI.LinearAlgebra
{
    public class DistributedVector
    {
        private readonly Vector localVector;
        private readonly bool isRhsVector;

        public DistributedVector(double[] localVector, bool isRhsVector)
        {
            this.localVector = Vector.CreateFromArray(localVector, false);
            this.isRhsVector = isRhsVector;
        }

        public DistributedVector(Vector localVector, bool isRhsVector)
        {
            this.localVector = localVector;
            this.isRhsVector = isRhsVector;
        }

        public DistributedVector Copy() => new DistributedVector(localVector.Copy(), isRhsVector);

        public void AxpyIntoThis(DistributedVector otherVector, double otherCoefficient)
            => this.localVector.AxpyIntoThis(otherVector.localVector, otherCoefficient);

        public void LinearCombinationIntoThis(double thisCoefficient, DistributedVector otherVector, double otherCoefficient)
            => this.localVector.LinearCombinationIntoThis(thisCoefficient, otherVector.localVector, otherCoefficient);

        public double DotProduct(IVectorView vector)
        {
            throw new NotImplementedException();
            if (isRhsVector)
            {

            }
            else
            {
                
            }
        }

    }
}
