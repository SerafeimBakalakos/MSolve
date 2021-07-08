using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Iterative;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.LinearAlgebraExtensions
{
    public interface IVectorMultipliable : ILinearTransformation
    {
        IVector Multiply(IVectorView lhsVector);
    }
}
