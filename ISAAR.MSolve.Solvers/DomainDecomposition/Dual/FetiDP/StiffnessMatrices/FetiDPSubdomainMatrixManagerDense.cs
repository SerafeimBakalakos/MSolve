﻿
using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Triangulation;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.Assemblers;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.LinearSystems;
using ISAAR.MSolve.Solvers.Ordering.Reordering;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices
{
    /// <summary>
    /// Dense format for Kii, Kbi/Kib, Kbb, Krr, Krc/Kcr, Kcc, KccStar and Skyline for Kff.
    /// Useful during prototyping and for debugging. For performance the other alternatives are probably better.
    /// Authors: Serafeim Bakalakos
    /// </summary>
    public class FetiDPSubdomainMatrixManagerDense : FetiDPSubdomainMatrixManagerBase
    {
        private readonly SkylineAssembler assembler = new SkylineAssembler();
        private readonly SingleSubdomainSystem<SkylineMatrix> linearSystem;

        private DiagonalMatrix inverseKiiDiagonal;
        private Matrix inverseKii;
        private CholeskyFull inverseKrr;
        private Matrix Kbb;
        private Matrix Kbi;
        private Matrix Kcc; //TODO: This can be overwritten with KccStar. Not high priority, since it is a small matrix.
        private Matrix _KccStar; 
        private Matrix Krc;
        private Matrix Krr;

        public FetiDPSubdomainMatrixManagerDense(ISubdomain subdomain, IFetiDPDofSeparator dofSeparator) : 
            base(subdomain, dofSeparator, null)
        {
            this.linearSystem = new SingleSubdomainSystem<SkylineMatrix>(subdomain);
        }

        protected override IMatrixView KccStarImpl => this._KccStar;

        public override ISingleSubdomainLinearSystem LinearSystem => linearSystem;

        protected override IMatrix BuildFreeDofsMatrixImpl(ISubdomainFreeDofOrdering dofOrdering, IEnumerable<IElement> elements,
            IElementMatrixProvider matrixProvider)
            => assembler.BuildGlobalMatrix(dofOrdering, elements, matrixProvider);


        protected override (IMatrix Kff, IMatrixView Kfc, IMatrixView Kcf, IMatrixView Kcc) BuildFreeConstrainedMatricesImpl(
            ISubdomainFreeDofOrdering freeDofOrdering, ISubdomainConstrainedDofOrdering constrainedDofOrdering,
            IEnumerable<IElement> elements, IElementMatrixProvider matrixProvider)
            => assembler.BuildGlobalSubmatrices(freeDofOrdering, constrainedDofOrdering, elements, matrixProvider);

        protected override void CalcInverseKiiImpl(bool diagonalOnly)
        {
            int[] internalDofs = dofSeparator.GetInternalDofIndices(subdomain);
            if (diagonalOnly)
            {
                var diagonal = new double[internalDofs.Length];
                for (int i = 0; i < diagonal.Length; ++i)
                {
                    int idx = internalDofs[i];
                    diagonal[i] = 1.0 / Krr[idx, idx];
                    //diagonal[i] = Krr[idx, idx];
                }
                inverseKiiDiagonal = DiagonalMatrix.CreateFromArray(diagonal, false);
                //inverseKiiDiagonal.Invert();
            }
            else
            {
                inverseKii = Krr.GetSubmatrix(internalDofs, internalDofs);
                inverseKii.InvertInPlace();
            }
        }

        protected override void ClearMatricesImpl()
        {
            inverseKii = null;
            inverseKiiDiagonal = null;
            inverseKrr = null;
            Kbb = null;
            Kbi = null;
            Kcc = null;
            Krc = null;
            Krr = null;
            _KccStar = null;
            //linearSystem.Matrix = null; // DO NOT DO THAT!!! The analyzer manages that.
        }

        protected override void CondenseMatricesStaticallyImpl()
        {
            // KccStar[s] = Kcc[s] - Krc[s]^T * inv(Krr[s]) * Krc[s]
            _KccStar = Kcc - Krc.MultiplyRight(inverseKrr.SolveLinearSystems(Krc), true);
        }

        protected override void ExtractKbbImpl() => Kbb = Krr.GetSubmatrix(DofsBoundary, DofsBoundary);
        protected override void ExtractKbiKibImpl() => Kbi = Krr.GetSubmatrix(DofsBoundary, DofsInternal);
        protected override void ExtractCornerRemainderSubmatricesImpl()
        {
            Kcc = linearSystem.Matrix.GetSubmatrixFull(DofsCorner, DofsCorner);
            Krc = linearSystem.Matrix.GetSubmatrixFull(DofsRemainder, DofsCorner);
            Krr = linearSystem.Matrix.GetSubmatrixFull(DofsRemainder, DofsRemainder);
        }

        public override void HandleDofOrderingWillBeModified() => assembler.HandleDofOrderingWillBeModified();

        protected override void InvertKrrImpl(bool inPlace) => inverseKrr = Krr.FactorCholesky(inPlace);

        protected override Vector MultiplyInverseKiiTimesImpl(Vector vector, bool diagonalOnly)
            => diagonalOnly ? inverseKiiDiagonal * vector : inverseKii * vector;

        protected override Matrix MultiplyInverseKiiTimesImpl(Matrix matrix, bool diagonalOnly)
            => diagonalOnly ? inverseKiiDiagonal * matrix : inverseKii * matrix;

        protected override Vector MultiplyInverseKrrTimesImpl(Vector vector) => inverseKrr.SolveLinearSystem(vector);

        protected override Vector MultiplyKbbTimesImpl(Vector vector) => Kbb * vector;
        protected override Matrix MultiplyKbbTimesImpl(Matrix matrix) => Kbb * matrix;
        protected override Vector MultiplyKbiTimesImpl(Vector vector) => Kbi * vector;
        protected override Matrix MultiplyKbiTimesImpl(Matrix matrix) => Kbi * matrix;
        protected override Vector MultiplyKccTimesImpl(Vector vector) => Kcc * vector;
        protected override Vector MultiplyKcrTimesImpl(Vector vector) => Krc.Multiply(vector, true);
        protected override Vector MultiplyKibTimesImpl(Vector vector) => Kbi.Multiply(vector, true);
        protected override Matrix MultiplyKibTimesImpl(Matrix matrix) => Kbi.MultiplyRight(matrix, true);
        protected override Vector MultiplyKrcTimesImpl(Vector vector) => Krc.Multiply(vector);

        protected override DofPermutation ReorderInternalDofsImpl()
        {
            // Do nothing, since the sparsity pattern is irrelevant for dense matrices.
            return DofPermutation.CreateNoPermutation();
        }

        protected override DofPermutation ReorderRemainderDofsImpl()
        {
            // Do nothing, since the sparsity pattern is irrelevant for dense matrices.
            return DofPermutation.CreateNoPermutation();
        }
    }
}