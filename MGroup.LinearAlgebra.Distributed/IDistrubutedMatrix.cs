using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.LinearAlgebra.Distributed
{
    public interface IDistrubutedMatrix
    {
        void Multiply(IDistributedVector lhs, IDistributedVector rhs);
    }
}
