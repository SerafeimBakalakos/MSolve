using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ISAAR.MSolve.Numerical.Optimization.Initialization
{
    public interface IInitializer<T>
    {
        T[] Generate();
    }
}
