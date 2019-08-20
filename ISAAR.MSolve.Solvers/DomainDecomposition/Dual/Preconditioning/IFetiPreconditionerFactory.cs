using System.Collections.Generic;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Solvers.DomainDecomposition.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Pcg;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.StiffnessDistribution;

//TODO: Also add ReorderInternalDofsForMultiplication and ReorderBoundaryDofsForMultiplication
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Preconditioning
{
    public interface IFetiPreconditionerFactory
    {
        bool ReorderInternalDofsForFactorization { get; }

        IFetiPreconditioner CreatePreconditioner(IModel model, IStiffnessDistribution stiffnessDistribution,
            IDofSeparator dofSeparator, ILagrangeMultipliersEnumerator lagrangeEnumerator, 
            Dictionary<int, IFetiSubdomainMatrixManager> matrixManagers);
    }
}
