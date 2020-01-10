﻿using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Builders;
using ISAAR.MSolve.LinearAlgebra.Reordering;
using ISAAR.MSolve.LinearAlgebra.SchurComplements;
using ISAAR.MSolve.LinearAlgebra.Triangulation;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.Assemblers;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.LinearSystems;
using ISAAR.MSolve.Solvers.Ordering.Reordering;

//TODO: Kff should probably be a DOK. It will only be used to extract Krr, Krc, Kcc. This avoids dependencies between factorizing
//      Krr and calculating the preconditioners. Boundary/internal dofs should be (also) defined with respect to Kff.
//      What about dynamic problems, where Kff needs to do linear combinations and matrix-vector multiplications
//TODO: This should be IDisposable. Probably IFetiSubdomainMatrixManager should be too.
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.StiffnessMatrices
{
    /// <summary>
    /// Dense format for Kbb, symmetric packed for Kcc, KccStar, skyline for Kff, Krr, Kii and CSC for Kib, Krc.
    /// Authors: Serafeim Bakalakos
    /// </summary>
    public class FetiDPSubdomainMatrixManagerSuiteSparse : FetiDPSubdomainMatrixManagerBase
    { 
        private readonly SymmetricDokAssembler assembler = new SymmetricDokAssembler();
        private readonly SingleSubdomainSystemMpi<DokSymmetric> linearSystem;

        private DiagonalMatrix inverseKiiDiagonal;
        private CholeskySuiteSparse inverseKii;
        private CholeskySuiteSparse inverseKrr;
        private Matrix Kbb;
        private CscMatrix Kib;
        private SymmetricMatrix Kcc;
        //TODO: This can be overwritten with KccStar. Not high priority, since it is a small matrix.
        private SymmetricMatrix _KccStar;
        private CscMatrix Krc;
        private DokSymmetric Krr; //TODO: Perhaps I should only use this for the static condensation and then discard it. Kff will be used for Kbb, Kib, Kii

        public FetiDPSubdomainMatrixManagerSuiteSparse(ISubdomain subdomain, IFetiDPDofSeparator dofSeparator, 
            IReorderingAlgorithm reordering) : base(subdomain, dofSeparator, reordering)
        {
            this.linearSystem = new SingleSubdomainSystemMpi<DokSymmetric>(subdomain);
        }

        public override ISingleSubdomainLinearSystemMpi LinearSystem => linearSystem;

        protected override IMatrixView KccStarImpl => this._KccStar;

        protected override void BuildFreeDofsMatrixImpl(ISubdomainFreeDofOrdering dofOrdering,
            IElementMatrixProvider matrixProvider)
        {
            linearSystem.Matrix = assembler.BuildGlobalMatrix(dofOrdering, 
                linearSystem.Subdomain.EnumerateElements(), matrixProvider);
        }

        protected override (IMatrix Kff, IMatrixView Kfc, IMatrixView Kcf, IMatrixView Kcc) BuildFreeConstrainedMatricesImpl(
            ISubdomainFreeDofOrdering freeDofOrdering, ISubdomainConstrainedDofOrdering constrainedDofOrdering,
            IEnumerable<IElement> elements, IElementMatrixProvider matrixProvider)
            => throw new NotImplementedException();

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
                SymmetricCscMatrix Kii = Krr.GetSubmatrixSymmetricDok(internalDofs).BuildSymmetricCscMatrix(true);
                if (inverseKii != null) inverseKii.Dispose();
                inverseKii = CholeskySuiteSparse.Factorize(Kii, true);
            }
        }

        protected override void ClearMatricesImpl()
        {
            if (inverseKii != null) inverseKii.Dispose();
            inverseKii = null;
            inverseKiiDiagonal = null;
            if (inverseKrr != null) inverseKrr.Dispose();
            inverseKrr = null;
            Kbb = null;
            Kib = null;
            Kcc = null;
            Krc = null;
            Krr = null;
            _KccStar = null;
            //linearSystem.Matrix = null; // DO NOT DO THAT!!! The analyzer manages that.
        }

        protected override void CondenseMatricesStaticallyImpl()
        {
            // KccStar[s] = Kcc[s] - Krc[s]^T * inv(Krr[s]) * Krc[s]
            _KccStar = SchurComplementCsc.CalcSchurComplementSymmetric(Kcc, Krc, inverseKrr);
        }

        protected override void ExtractKbbImpl() => Kbb = Krr.GetSubmatrixSymmetricFull(DofsBoundary);
        protected override void ExtractKbiKibImpl() 
            => Kib = Krr.GetSubmatrixDokColMajor(DofsInternal, DofsBoundary).BuildCscMatrix(true);
        protected override void ExtractCornerRemainderSubmatricesImpl()
        {
            Kcc = linearSystem.Matrix.GetSubmatrixSymmetricPacked(DofsCorner);
            Krc = linearSystem.Matrix.GetSubmatrixDokColMajor(DofsRemainder, DofsCorner).BuildCscMatrix(true);
            Krr = linearSystem.Matrix.GetSubmatrixSymmetricDok(DofsRemainder);
        }

        public override void HandleDofOrderingWillBeModified() => assembler.HandleDofOrderingWillBeModified();

        protected override void InvertKrrImpl(bool inPlace)
        {
            if (inverseKrr != null) inverseKrr.Dispose();
            inverseKrr = CholeskySuiteSparse.Factorize(Krr.BuildSymmetricCscMatrix(true), true);
        }

        protected override Vector MultiplyInverseKiiTimesImpl(Vector vector, bool diagonalOnly)
            => diagonalOnly ? inverseKiiDiagonal * vector : inverseKii.SolveLinearSystem(vector);

        protected override Matrix MultiplyInverseKiiTimesImpl(Matrix matrix, bool diagonalOnly)
            => diagonalOnly ? inverseKiiDiagonal * matrix : inverseKii.SolveLinearSystems(matrix);

        protected override Vector MultiplyInverseKrrTimesImpl(Vector vector) => inverseKrr.SolveLinearSystem(vector);

        protected override Vector MultiplyKbbTimesImpl(Vector vector) => Kbb * vector;
        protected override Matrix MultiplyKbbTimesImpl(Matrix matrix) => Kbb * matrix;
        protected override Vector MultiplyKbiTimesImpl(Vector vector) => Kib.Multiply(vector, true);
        protected override Matrix MultiplyKbiTimesImpl(Matrix matrix) => Kib.MultiplyRight(matrix, true);
        protected override Vector MultiplyKccTimesImpl(Vector vector) => Kcc * vector;
        protected override Vector MultiplyKcrTimesImpl(Vector vector) => Krc.Multiply(vector, true);
        protected override Vector MultiplyKibTimesImpl(Vector vector) => Kib.Multiply(vector);
        protected override Matrix MultiplyKibTimesImpl(Matrix matrix) => Kib.MultiplyRight(matrix);
        protected override Vector MultiplyKrcTimesImpl(Vector vector) => Krc.Multiply(vector);

        protected override DofPermutation ReorderInternalDofsImpl()
        {
            SparsityPatternSymmetric pattern = Krr.GetSubmatrixSymmetricPattern(DofsInternal);
            (int[] permutation, bool oldToNew) = reordering.FindPermutation(pattern);
            return DofPermutation.Create(permutation, oldToNew);
        }

        protected override DofPermutation ReorderRemainderDofsImpl()
        {
            SparsityPatternSymmetric pattern = linearSystem.Matrix.GetSubmatrixSymmetricPattern(DofsRemainder);
            (int[] permutation, bool oldToNew) = reordering.FindPermutation(pattern);
            return DofPermutation.Create(permutation, oldToNew);
        }
    }
}
