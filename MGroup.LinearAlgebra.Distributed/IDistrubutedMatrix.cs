using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.LinearAlgebra.Distributed
{
    public interface IDistributedMatrix
    {
        void Multiply(IDistributedVector lhs, IDistributedVector rhs);
    }
}
