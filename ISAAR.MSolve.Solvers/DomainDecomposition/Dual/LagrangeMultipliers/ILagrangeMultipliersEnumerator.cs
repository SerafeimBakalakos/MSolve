using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers
{
    public interface ILagrangeMultipliersEnumerator
    {
        Dictionary<int, SignedBooleanMatrixColMajor> BooleanMatrices { get; }

        /// <summary>
        /// Associates each lagrange multiplier with the instances of the boundary dof, for which continuity is enforced. 
        /// </summary>
        LagrangeMultiplier[] LagrangeMultipliers { get; }

        int NumLagrangeMultipliers { get; }
    }
}
