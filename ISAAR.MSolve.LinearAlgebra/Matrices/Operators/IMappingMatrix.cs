using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;

//TODO: What about transpose(this) * other * this operations? More importantly, what about accessing rows2cols (or cols2rows) map? 
namespace ISAAR.MSolve.LinearAlgebra.Matrices.Operators
{
    public interface IMappingMatrix : IBounded2D
    {
        Vector Multiply(Vector vector, bool transposeThis = false);
        Matrix MultiplyRight(Matrix other, bool transposeThis = false);
    }
}
