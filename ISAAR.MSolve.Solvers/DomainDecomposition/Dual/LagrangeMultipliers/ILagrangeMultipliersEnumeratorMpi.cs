using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers
{
    public interface ILagrangeMultipliersEnumeratorMpi
    {
        SignedBooleanMatrixColMajor BooleanMatrix { get; }

        /// <summary>
        /// Associates each lagrange multiplier with the instances of the boundary dof, for which continuity is enforced.
        /// This is only available in the master process, since other processes do not have access to the nodes where the 
        /// lagrange multipliers are applied.
        /// </summary>
        LagrangeMultiplier[] LagrangeMultipliers { get; }

        int NumLagrangeMultipliers { get; }

    }
}
