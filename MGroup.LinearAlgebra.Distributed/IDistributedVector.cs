using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.LinearAlgebra.Distributed
{
    public interface IDistributedVector
    {
        IDistributedVector Copy();

        IDistributedVector CreateZeroVectorWithSameFormat();
    }
}
