using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.Solvers.Distributed.LinearAlgebra
{
    public interface IIterativeMethodMatrix
    {
        void MultiplyIntoResult(IIterativeMethodVector lhsVector, IIterativeMethodVector rhsVector);
    }
}
