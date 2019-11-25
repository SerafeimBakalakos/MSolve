using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.LinearAlgebra.MPI;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Pcg;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.StiffnessDistribution;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Preconditioning
{
    public class FetiPreconditionerMpi : IFetiPreconditioner
    {
        private readonly IMappingMatrix Bpb;
        private readonly IFetiSubdomainMatrixManager matrixManager;
        private readonly ILagrangeMultipliersEnumerator lagrangesEnumerator;
        private readonly IFetiPreconditioningOperations preconditioning;
        private readonly ProcessDistribution procs;

        private FetiPreconditionerMpi(ProcessDistribution processDistribution, IFetiPreconditioningOperations preconditioning,
            IModel model, IDofSeparator dofSeparator, ILagrangeMultipliersEnumerator lagrangesEnumerator,
            IFetiMatrixManager matrixManager, IStiffnessDistribution stiffnessDistribution)
        {

            ISubdomain subdomain = model.GetSubdomain(processDistribution.OwnSubdomainID);
            this.procs = processDistribution;
            this.preconditioning = preconditioning;
            this.lagrangesEnumerator = lagrangesEnumerator;
            this.matrixManager = matrixManager.GetSubdomainMatrixManager(subdomain);

            this.Bpb = PreconditioningUtilities.CalcBoundaryPreconditioningBooleanMatrix(
                subdomain, dofSeparator, lagrangesEnumerator, stiffnessDistribution); //TODO: When can these ones be reused?
            preconditioning.PrepareSubdomainSubmatrices(matrixManager.GetSubdomainMatrixManager(subdomain));
        }

        public void SolveLinearSystem(Vector rhs, Vector lhs)
        {
            procs.Communicator.BroadcastVector(ref rhs, lagrangesEnumerator.NumLagrangeMultipliers, procs.MasterProcess);
            Vector subdomainContribution = preconditioning.PreconditionSubdomainVector(rhs, matrixManager, Bpb);
            procs.Communicator.SumVector(subdomainContribution, lhs, procs.MasterProcess);
        }

        public void SolveLinearSystems(Matrix rhs, Matrix lhs)
        {
            procs.Communicator.BroadcastMatrix(ref rhs, procs.MasterProcess);
            Matrix subdomainContribution = preconditioning.PreconditionSubdomainMatrix(rhs, matrixManager, Bpb);
            procs.Communicator.SumMatrix(subdomainContribution, lhs, procs.MasterProcess);
        }

        public class Factory : IFetiPreconditionerFactory
        {
            private readonly ProcessDistribution procs;

            public Factory(ProcessDistribution processDistribution)
            {
                this.procs = processDistribution;
            }

            public IFetiPreconditioner CreatePreconditioner(IFetiPreconditioningOperations preconditioning,
                IModel model, IDofSeparator dofSeparator, ILagrangeMultipliersEnumerator lagrangeEnumerator,
                IFetiMatrixManager matrixManager, IStiffnessDistribution stiffnessDistribution)
            {
                return new FetiPreconditionerMpi(procs, preconditioning, model, dofSeparator, lagrangeEnumerator, matrixManager, 
                    stiffnessDistribution);
            }
        }
    }
}
