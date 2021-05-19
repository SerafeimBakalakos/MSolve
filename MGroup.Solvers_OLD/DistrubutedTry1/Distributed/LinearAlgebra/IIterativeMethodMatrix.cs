using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.Solvers_OLD.DistributedTry1.Distributed.LinearAlgebra
{
    public interface IIterativeMethodMatrix
    {
        void MultiplyIntoResult(IIterativeMethodVector lhsVector, IIterativeMethodVector rhsVector);
    }
}
