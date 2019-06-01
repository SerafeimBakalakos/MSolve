﻿using System.Collections.Generic;
using System.Linq;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Pcg;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.StiffnessDistribution;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Preconditioning
{
    public class DirichletPreconditioner : IFetiPreconditioner
    {
        private readonly Dictionary<int, IMappingMatrix> preconditioningBoundarySignedBooleanMatrices;
        private readonly Dictionary<int, Matrix> stiffnessesBoundaryBoundary;
        private readonly Dictionary<int, Matrix> stiffnessesBoundaryInternal;
        private readonly Dictionary<int, Matrix> stiffnessesInternalInternalInverse;
        private readonly int[] subdomainIDs;

        private DirichletPreconditioner(int[] subdomainIDs, Dictionary<int, Matrix> stiffnessesBoundaryBoundary,
            Dictionary<int, Matrix> stiffnessesBoundaryInternal, Dictionary<int, Matrix> stiffnessesInternalInternalInverse,
            Dictionary<int, IMappingMatrix> preconditioningBoundarySignedBooleanMatrices)
        {
            this.subdomainIDs = subdomainIDs;
            this.preconditioningBoundarySignedBooleanMatrices = preconditioningBoundarySignedBooleanMatrices;
            this.stiffnessesBoundaryBoundary = stiffnessesBoundaryBoundary;
            this.stiffnessesBoundaryInternal = stiffnessesBoundaryInternal;
            this.stiffnessesInternalInternalInverse = stiffnessesInternalInternalInverse;
        }

        public void SolveLinearSystem(Vector rhs, Vector lhs)
        {
            lhs.Clear(); //TODO: this should be avoided
            foreach (int id in subdomainIDs)
            {
                IMappingMatrix Bpb = preconditioningBoundarySignedBooleanMatrices[id];
                Matrix Kbb = stiffnessesBoundaryBoundary[id];
                Matrix Kbi = stiffnessesBoundaryInternal[id];
                Matrix invKii = stiffnessesInternalInternalInverse[id];

                // inv(F) * y = Bpb * S * Bpb^T * y
                // S = Kbb - Kbi * inv(Kii) * Kib
                Vector By = Bpb.Multiply(rhs, true);
                Vector SBy = Kbb.Multiply(By) - Kbi.Multiply(invKii.Multiply(Kbi.Multiply(By, true)));
                Vector subdomainContribution = Bpb.Multiply(SBy);
                lhs.AddIntoThis(subdomainContribution);
            }
        }

        public void SolveLinearSystems(Matrix rhs, Matrix lhs)
        {
            lhs.Clear(); //TODO: this should be avoided
            foreach (int id in subdomainIDs)
            {
                IMappingMatrix Bpb = preconditioningBoundarySignedBooleanMatrices[id];
                Matrix Kbb = stiffnessesBoundaryBoundary[id];
                Matrix Kbi = stiffnessesBoundaryInternal[id];
                Matrix invKii = stiffnessesInternalInternalInverse[id];

                // inv(F) * Y = Bpb * S * Bpb^T * Y
                // S = Kbb - Kbi * inv(Kii) * Kib
                Matrix BY = Bpb.MultiplyRight(rhs, true);
                Matrix SBY = Kbb.MultiplyRight(BY) - Kbi.MultiplyRight(invKii.MultiplyRight(Kbi.MultiplyRight(BY, true)));
                Matrix subdomainContribution = Bpb.MultiplyRight(SBY);
                lhs.AddIntoThis(subdomainContribution);
            }
        }

        public class Factory : FetiPreconditionerFactoryBase
        {
            public override IFetiPreconditioner CreatePreconditioner(IStiffnessDistribution stiffnessDistribution,
                IDofSeparator dofSeparator, ILagrangeMultipliersEnumerator lagrangeEnumerator,
                Dictionary<int, IMatrixView> stiffnessMatrices)
            {
                int[] subdomainIDs = dofSeparator.BoundaryDofIndices.Keys.ToArray();
                Dictionary<int, IMappingMatrix> boundaryBooleans = CalcBoundaryPreconditioningBooleanMatrices(
                    stiffnessDistribution, dofSeparator, lagrangeEnumerator);
                Dictionary<int, Matrix> stiffnessesBoundaryBoundary = 
                    ExtractStiffnessesBoundaryBoundary(dofSeparator, stiffnessMatrices);
                Dictionary<int, Matrix> stiffnessesBoundaryInternal = 
                    ExtractStiffnessBoundaryInternal(dofSeparator, stiffnessMatrices);
                Dictionary<int, Matrix> stiffnessesInternalInternalInverse = 
                    InvertStiffnessInternalInternal(dofSeparator.InternalDofIndices, stiffnessMatrices);

                return new DirichletPreconditioner(subdomainIDs, stiffnessesBoundaryBoundary, stiffnessesBoundaryInternal, 
                    stiffnessesInternalInternalInverse, boundaryBooleans);
            }

            private Dictionary<int, Matrix> InvertStiffnessInternalInternal(Dictionary<int, int[]> internalDofs, 
                Dictionary<int, IMatrixView> stiffnessMatrices)
            {
                var stiffnessesInternalInternalInverse = new Dictionary<int, Matrix>();
                foreach (int id in internalDofs.Keys)
                {
                    Matrix stiffnessInternalInternal = stiffnessMatrices[id].GetSubmatrix(internalDofs[id], internalDofs[id]);
                    stiffnessInternalInternal.InvertInPlace();
                    stiffnessesInternalInternalInverse.Add(id, stiffnessInternalInternal);
                }
                return stiffnessesInternalInternalInverse;
            }
        }
    }
}
