﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Pcg;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.StiffnessDistribution;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Preconditioning
{
    public class DiagonalDirichletPreconditioner : IFetiPreconditioner
    {
        private readonly Dictionary<int, IFetiSubdomainMatrixManager> matrixManagers;
        private readonly Dictionary<int, IMappingMatrix> preconditioningBoundarySignedBooleanMatrices;
        private readonly int[] subdomainIDs;

        private DiagonalDirichletPreconditioner(int[] subdomainIDs, Dictionary<int, IFetiSubdomainMatrixManager> matrixManagers,
            Dictionary<int, IMappingMatrix> preconditioningBoundarySignedBooleanMatrices)
        {
            this.subdomainIDs = subdomainIDs;
            this.matrixManagers = matrixManagers;
            this.preconditioningBoundarySignedBooleanMatrices = preconditioningBoundarySignedBooleanMatrices;
        }

        public void SolveLinearSystem(Vector rhs, Vector lhs)
        {
            lhs.Clear(); //TODO: this should be avoided
            foreach (int s in subdomainIDs)
            {
                IFetiSubdomainMatrixManager matrixManager = matrixManagers[s];
                IMappingMatrix Bpb = preconditioningBoundarySignedBooleanMatrices[s];

                // inv(F) * y = Bpb * S * Bpb^T * y
                // S = Kbb - Kbi * inv(Dii) * Kib
                Vector By = Bpb.Multiply(rhs, true);
                Vector temp = matrixManager.MultiplyKibTimes(By);
                temp = matrixManager.MultiplyInverseKiiDiagonalTimes(temp);
                temp = matrixManager.MultiplyKbiTimes(temp);
                temp = matrixManager.MultiplyKbbTimes(By) - temp;
                Vector subdomainContribution = Bpb.Multiply(temp);
                lhs.AddIntoThis(subdomainContribution);
            }
        }

        public void SolveLinearSystems(Matrix rhs, Matrix lhs)
        {
            lhs.Clear(); //TODO: this should be avoided
            foreach (int s in subdomainIDs)
            {
                IFetiSubdomainMatrixManager matrixManager = matrixManagers[s];
                IMappingMatrix Bpb = preconditioningBoundarySignedBooleanMatrices[s];

                // inv(F) * Y =  Bpb * S * Bpb^T * Y
                // S = Kbb - Kbi * inv(Dii) * Kib
                Matrix BY = Bpb.MultiplyRight(rhs, true);
                Matrix temp = matrixManager.MultiplyKibTimes(BY);
                temp = matrixManager.MultiplyInverseKiiDiagonalTimes(temp);
                temp = matrixManager.MultiplyKbiTimes(temp);
                temp = matrixManager.MultiplyKbbTimes(BY) - temp;
                Matrix subdomainContribution = Bpb.MultiplyRight(temp);
                lhs.AddIntoThis(subdomainContribution);
            }
        }

        public class Factory : FetiPreconditionerFactoryBase
        {
            public override bool ReorderInternalDofsForFactorization => false;

            public override IFetiPreconditioner CreatePreconditioner(IModel model,
                IStiffnessDistribution stiffnessDistribution, IDofSeparator dofSeparator,
                ILagrangeMultipliersEnumerator lagrangeEnumerator, Dictionary<int, IFetiSubdomainMatrixManager> matrixManagers)
            {
                int[] subdomainIDs = matrixManagers.Keys.ToArray();
                Dictionary<int, IMappingMatrix> boundaryBooleans = CalcBoundaryPreconditioningBooleanMatrices(model, 
                    stiffnessDistribution, dofSeparator, lagrangeEnumerator);

                foreach (int s in subdomainIDs)
                {
                    ISubdomain subdomain = model.GetSubdomain(s);
                    if (!subdomain.StiffnessModified) continue;
                    Debug.WriteLine($"{typeof(DiagonalDirichletPreconditioner).Name}.{this.GetType().Name}:" 
                        + $" Extracting boundary/internal submatrices of subdomain {s} for preconditioning");
                    IFetiSubdomainMatrixManager matrixManager = matrixManagers[s];
                    int[] boundaryDofs = dofSeparator.GetBoundaryDofIndices(subdomain);
                    int[] internalDofs = dofSeparator.GetInternalDofIndices(subdomain);
                    matrixManager.ExtractKbb(boundaryDofs);
                    matrixManager.ExtractKbiKib(boundaryDofs, internalDofs);
                    matrixManager.ExtractAndInvertKiiDiagonal(internalDofs);
                }
                return new DiagonalDirichletPreconditioner(subdomainIDs, matrixManagers, boundaryBooleans);
            }
        }
    }
}
