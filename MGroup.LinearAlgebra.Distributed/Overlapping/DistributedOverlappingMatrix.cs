using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Exceptions;

//TODO: Actually if IDistributedVector exposes its local subvectors, it probably does not matter if the lhs vector is 
//      DistributedOverlappingVector.
namespace MGroup.LinearAlgebra.Distributed.Overlapping
{
    public class DistributedOverlappingMatrix : IDistrubutedMatrix
    {
        public void Multiply(IDistributedVector lhs, IDistributedVector rhs)
        {
            if ((lhs is DistributedOverlappingVector lhsCasted) && (rhs is DistributedOverlappingVector rhsCasted))
            {
                Multiply(lhsCasted, rhsCasted);
            }
            else
            {
                throw new ArgumentException(
                    "This operation is legal only if the left-hand-side and righ-hand-side vectors are distributed" +
                    " with overlapping entries.");
            }
            throw new NotImplementedException();
        }

        public void Multiply(DistributedOverlappingVector lhs, DistributedOverlappingVector rhs)
        {
            throw new NotImplementedException();
        }
    }
}
