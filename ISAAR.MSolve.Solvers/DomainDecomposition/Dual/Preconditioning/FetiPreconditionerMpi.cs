using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Pcg;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.StiffnessDistribution;
using MPI;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Preconditioning
{
    public class FetiPreconditionerMpi : IFetiPreconditioner
    {
        private readonly IFetiSubdomainMatrixManager matrixManager;
        private readonly IMappingMatrix Bpb;
        private readonly IFetiPreconditioningOperations preconditioning;
        private readonly ProcessDistribution procs;

        private FetiPreconditionerMpi(ProcessDistribution processDistribution, IFetiPreconditioningOperations preconditioning,
            IModel model, IDofSeparator dofSeparator, ILagrangeMultipliersEnumerator lagrangeEnumerator,
            IFetiMatrixManager matrixManager, IStiffnessDistribution stiffnessDistribution)
        {

            ISubdomain subdomain = model.GetSubdomain(processDistribution.OwnSubdomainID);
            this.procs = processDistribution;
            this.preconditioning = preconditioning;
            this.matrixManager = matrixManager.GetSubdomainMatrixManager(subdomain);

            this.Bpb = PreconditioningUtilities.CalcBoundaryPreconditioningBooleanMatrix(
                subdomain, dofSeparator, lagrangeEnumerator, stiffnessDistribution); //TODO: When can these ones be reused?
            preconditioning.PrepareSubdomainSubmatrices(matrixManager.GetSubdomainMatrixManager(subdomain));
        }

        public void SolveLinearSystem(Vector rhs, Vector lhs)
        {
            BroadcastVector(ref rhs);
            Vector subdomainContribution = preconditioning.PreconditionSubdomainVector(rhs, matrixManager, Bpb);
            ReduceVector(subdomainContribution, lhs);
        }

        public void SolveLinearSystems(Matrix rhs, Matrix lhs)
        {
            BroadcastMatrix(ref rhs);
            Matrix subdomainContribution = preconditioning.PreconditionSubdomainMatrix(rhs, matrixManager, Bpb);
            ReduceMatrix(subdomainContribution, lhs);
        }

        private void BroadcastMatrix(ref Matrix matrix)
        {
            //TODO: Use a dedicated class for MPI communication of Matrix. This class belongs to a project LinearAlgebra.MPI.
            //      Avoid the automatic serialization of MPI.NET.
            procs.Communicator.Broadcast<Matrix>(ref matrix, procs.MasterProcess);
        }

        private void BroadcastVector(ref Vector vector)
        {
            //TODO: Use a dedicated class for MPI communication of Vector. This class belongs to a project LinearAlgebra.MPI.
            //      Avoid copying the array.
            double[] asArray = null;
            if (procs.IsMasterProcess) asArray = vector.CopyToArray();
            procs.Communicator.Broadcast<double>(ref asArray, procs.MasterProcess);
            vector = Vector.CreateFromArray(asArray);
        }

        private void ReduceMatrix(Matrix subdomainMatrix, Matrix globalMatrix)
        {
            //TODO: Use a dedicated class for MPI communication of Matrix.This class belongs to a project LinearAlgebra.MPI.
            //      Avoid the automatic serialization of MPI.NET and use built-in reductions which are much faster.

            ReductionOperation<Matrix> matrixAddition = (A, B) => A + B;
            Matrix sum = procs.Communicator.Reduce<Matrix>(subdomainMatrix, matrixAddition, procs.MasterProcess);
            globalMatrix.CopyFrom(sum);
        }

        private void ReduceVector(Vector subdomainVector, Vector globalVector)
        {
            //TODO: Use a dedicated class for MPI communication of Vector. This class belongs to a project LinearAlgebra.MPI.
            //      Avoid copying the array.
            double[] asArray = subdomainVector.CopyToArray();
            double[] sum = procs.Communicator.Reduce<double>(asArray, Operation<double>.Add, procs.MasterProcess);
            if (procs.IsMasterProcess) globalVector.CopyFrom(Vector.CreateFromArray(sum));
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
