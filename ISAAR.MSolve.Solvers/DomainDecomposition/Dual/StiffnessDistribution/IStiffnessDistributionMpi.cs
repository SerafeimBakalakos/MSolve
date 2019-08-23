using System;
using System.Collections.Generic;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;

//TODO: This should be an enum class. There are only 2 possible cases.
//TODO: This should work for both FETI-1 and FETI-DP
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.StiffnessDistribution 
{
    public interface IStiffnessDistributionMpi
    {
        double[] CalcBoundaryDofCoefficients(ISubdomain subdomain);

        Dictionary<int, double> CalcBoundaryDofCoefficients(INode node, IDofType dofType); //TODO: Better return Dictionary<ISubdomain, double>

        IMappingMatrix CalcBoundaryPreconditioningSignedBooleanMatrices(ILagrangeMultipliersEnumerator lagrangeEnumerator, 
            ISubdomain subdomain, SignedBooleanMatrixColMajor boundarySignedBooleanMatrices); //TODO: LagrangeEnumerator is only useful for heterogeneous. It should be injected in that contructor.

        void Update();
    }
}
