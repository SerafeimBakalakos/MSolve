using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Exceptions;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Reordering;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.LinearSystems;
using ISAAR.MSolve.Solvers.Ordering.Reordering;

//TODO: I could make this generic on the matrix type, so that I can have the assembler and all operations that use it here.
//TODO: Checks could be debug only.
//TODO: The multiply methods could be done by this class but seeing the matrices a IMatrixView.
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.Matrices
{
    public abstract class FetiDPSubdomainMatrixManagerBase : IFetiDPSubdomainMatrixManager
    {
        protected readonly IFetiDPDofSeparator dofSeparator;
        protected readonly IReorderingAlgorithm reordering;
        protected readonly ISubdomain subdomain;

        private Vector fbc;
        private Vector fr;
        private Vector fcStar;

        private bool areKbiKibExtracted;
        private bool areKccKcrKrcKrrExtracted;
        private bool areKccKcrKrcKrrCondensed;
        private bool isKbbExtracted;
        private bool isKiiOrItsDiagonalInverted;
        private bool isKrrInverted;
        private bool isKrrOverwritten;

        protected FetiDPSubdomainMatrixManagerBase(ISubdomain subdomain, IFetiDPDofSeparator dofSeparator, 
            IReorderingAlgorithm reordering)
        {
            this.subdomain = subdomain;
            this.dofSeparator = dofSeparator;
            this.reordering = reordering;
        }

        protected int[] DofsCorner => dofSeparator.GetCornerDofIndices(subdomain);
        protected int[] DofsBoundary => dofSeparator.GetBoundaryDofIndices(subdomain);
        protected int[] DofsInternal => dofSeparator.GetInternalDofIndices(subdomain);
        protected int[] DofsRemainder => dofSeparator.GetRemainderDofIndices(subdomain);

        public Vector Fbc
        {
            get
            {
                if (fbc == null) throw new InvalidOperationException(
                    "The remainder and corner subvectors (Fr and Fbc) must be calculated first.");
                return fbc;
            }
        }

        public Vector Fr
        {
            get
            {
                if (fr == null) throw new InvalidOperationException(
                    "The remainder and corner subvectors (Fr and Fbc) must be calculated first.");
                return fr;
            }
        }

        public Vector FcStar
        {
            get
            {
                if (fcStar == null) throw new InvalidOperationException(
                    "The remainder and corner subvectors (Fr and Fbc) must be condensed into FcStar first.");
                return FcStar;
            }
        }

        public IMatrixView KccStar
        {
            get
            {
                if (!areKccKcrKrcKrrCondensed) throw new InvalidOperationException(
                    "The remainder and corner submatrics (Kcc, Krc, Krc, Krr) must be condensed into KccStar first.");
                return KccStarImpl;
            }
        }
        protected abstract IMatrixView KccStarImpl {get;}

        public abstract ISingleSubdomainLinearSystem LinearSystem { get; }

        public IMatrix BuildFreeDofsMatrix(ISubdomainFreeDofOrdering dofOrdering, IEnumerable<IElement> elements, 
            IElementMatrixProvider matrixProvider)
        {
            ClearMatrices();
            return BuildFreeDofsMatrixImpl(dofOrdering, elements, matrixProvider);
        }
        protected abstract IMatrix BuildFreeDofsMatrixImpl(ISubdomainFreeDofOrdering dofOrdering, IEnumerable<IElement> elements,
            IElementMatrixProvider matrixProvider);

        public (IMatrix Kff, IMatrixView Kfc, IMatrixView Kcf, IMatrixView Kcc) BuildFreeConstrainedMatrices(
            ISubdomainFreeDofOrdering freeDofOrdering, ISubdomainConstrainedDofOrdering constrainedDofOrdering, 
            IEnumerable<IElement> elements, IElementMatrixProvider matrixProvider)
        {
            ClearMatrices();
            return BuildFreeConstrainedMatricesImpl(freeDofOrdering, constrainedDofOrdering, elements, matrixProvider);
        }
        protected abstract (IMatrix Kff, IMatrixView Kfc, IMatrixView Kcf, IMatrixView Kcc) BuildFreeConstrainedMatricesImpl(
            ISubdomainFreeDofOrdering freeDofOrdering, ISubdomainConstrainedDofOrdering constrainedDofOrdering,
            IEnumerable<IElement> elements, IElementMatrixProvider matrixProvider);

        public void CalcInverseKii(bool diagonalOnly)
        {
            CheckKrrAvailability();
            CalcInverseKiiImpl(diagonalOnly);
            isKiiOrItsDiagonalInverted = true;
        }
        protected abstract void CalcInverseKiiImpl(bool diagonalOnly);

        public void ClearMatrices()
        {
            areKbiKibExtracted = false;
            areKccKcrKrcKrrExtracted = false;
            areKccKcrKrcKrrCondensed = false;
            isKbbExtracted = false;
            isKiiOrItsDiagonalInverted = false;
            isKrrInverted = false;
            isKrrOverwritten = false;
            ClearMatricesImpl();
        }
        protected abstract void ClearMatricesImpl();

        public void ClearRhsVectors()
        {
            fbc = null;
            fr = null;
            fcStar = null;
        }

        public void CondenseMatricesStatically()
        {
            CheckKccKcrKrcKrrExtraction();
            CheckKrrInversion();
            CondenseMatricesStaticallyImpl();
            areKccKcrKrcKrrCondensed = true;
        }
        protected abstract void CondenseMatricesStaticallyImpl();

        public void CondenseRhsVectorsStatically()
        {
            // fcStar[s] = fbc[s] - Krc[s]^T * inv(Krr[s]) * fr[s]
            Vector temp = MultiplyInverseKrrTimes(Fr);
            temp = MultiplyKcrTimes(temp);
            fcStar = Fbc - temp;
        }

        public void ExtractCornerRemainderSubmatrices()
        {
            ClearMatrices();
            ExtractCornerRemainderSubmatricesImpl();
        }
        protected abstract void ExtractCornerRemainderSubmatricesImpl();

        public void ExtractCornerRemainderRhsSubvectors()
        {
            Vector Ff = LinearSystem.RhsConcrete;
            int[] cornerDofs = dofSeparator.GetCornerDofIndices(subdomain);
            int[] remainderDofs = dofSeparator.GetRemainderDofIndices(subdomain);
            fr = Ff.GetSubvector(remainderDofs);
            fbc = Ff.GetSubvector(cornerDofs);
            fcStar = null;
        }

        public void ExtractKbb()
        {
            CheckKrrAvailability();
            ExtractKbbImpl();
            isKbbExtracted = true;
        }
        protected abstract void ExtractKbbImpl();

        public void ExtractKbiKib()
        {
            CheckKrrAvailability();
            ExtractKbiKibImpl();
            areKbiKibExtracted = true;
        }
        protected abstract void ExtractKbiKibImpl();

        public abstract void HandleDofOrderingWillBeModified();

        public void InvertKrr(bool inPlace)
        {
            CheckKrrAvailability();
            InvertKrrImpl(inPlace);
            isKrrInverted = true;
            isKrrOverwritten = inPlace;
        }
        protected abstract void InvertKrrImpl(bool inPlace);

        public Vector MultiplyInverseKiiTimes(Vector vector, bool diagonalOnly)
        {
            CheckKiiOrItsDiagonalInversion();
            return MultiplyInverseKiiTimesImpl(vector, diagonalOnly);
        }
        protected abstract Vector MultiplyInverseKiiTimesImpl(Vector vector, bool diagonalOnly);

        public Matrix MultiplyInverseKiiTimes(Matrix matrix, bool diagonalOnly)
        {
            CheckKiiOrItsDiagonalInversion();
            return MultiplyInverseKiiTimesImpl(matrix, diagonalOnly);
        }
        protected abstract Matrix MultiplyInverseKiiTimesImpl(Matrix matrix, bool diagonalOnly);

        public Vector MultiplyInverseKrrTimes(Vector vector)
        {
            CheckKrrInversion();
            return MultiplyInverseKrrTimesImpl(vector);
        }
        protected abstract Vector MultiplyInverseKrrTimesImpl(Vector vector);

        public Vector MultiplyKbbTimes(Vector vector)
        {
            CheckKbbExtraction();
            return MultiplyKbbTimesImpl(vector);
        }
        protected abstract Vector MultiplyKbbTimesImpl(Vector vector);

        public Matrix MultiplyKbbTimes(Matrix matrix)
        {
            CheckKbbExtraction();
            return MultiplyKbbTimesImpl(matrix);
        }
        protected abstract Matrix MultiplyKbbTimesImpl(Matrix matrix);

        public Vector MultiplyKbiTimes(Vector vector)
        {
            CheckKbiKibExtraction();
            return MultiplyKbiTimesImpl(vector);
        }
        protected abstract Vector MultiplyKbiTimesImpl(Vector vector);

        public Matrix MultiplyKbiTimes(Matrix matrix)
        {
            CheckKbiKibExtraction();
            return MultiplyKbiTimesImpl(matrix);
        }
        protected abstract Matrix MultiplyKbiTimesImpl(Matrix matrix);

        public Vector MultiplyKccTimes(Vector vector)
        {
            CheckKccKcrKrcKrrExtraction();
            return MultiplyKccTimesImpl(vector);
        }
        protected abstract Vector MultiplyKccTimesImpl(Vector vector);

        public Vector MultiplyKcrTimes(Vector vector)
        {
            CheckKccKcrKrcKrrExtraction();
            return MultiplyKcrTimesImpl(vector);
        }
        protected abstract Vector MultiplyKcrTimesImpl(Vector vector);

        public Vector MultiplyKibTimes(Vector vector)
        {
            CheckKbiKibExtraction();
            return MultiplyKibTimesImpl(vector);
        }
        protected abstract Vector MultiplyKibTimesImpl(Vector vector);

        public Matrix MultiplyKibTimes(Matrix matrix)
        {
            CheckKbiKibExtraction();
            return MultiplyKibTimesImpl(matrix);
        }
        protected abstract Matrix MultiplyKibTimesImpl(Matrix matrix);

        public Vector MultiplyKrcTimes(Vector vector)
        {
            CheckKccKcrKrcKrrExtraction();
            return MultiplyKrcTimesImpl(vector);
        }
        protected abstract Vector MultiplyKrcTimesImpl(Vector vector);

        public DofPermutation ReorderInternalDofs()
        {
            if (reordering == null) return DofPermutation.CreateNoPermutation();
            CheckKrrAvailability();
            return ReorderInternalDofsImpl();
        }
        protected abstract DofPermutation ReorderInternalDofsImpl();

        public DofPermutation ReorderRemainderDofs()
        {
            if (reordering == null) return DofPermutation.CreateNoPermutation();
            try
            {
                return ReorderRemainderDofsImpl();
            }
            catch (MatrixDataOverwrittenException)
            {
                throw new InvalidOperationException(
                    "The free-free matrix of this subdomain has been overwritten and cannot be used anymore."
                    + "Try calling this method before factorizing/inverting it.");
            }
        }
        protected abstract DofPermutation ReorderRemainderDofsImpl();

        private void CheckKbbExtraction()
        {
            if (!isKbbExtracted) throw new InvalidOperationException(
                   "The boundary-remainder submatrix (Kbb) must be calculated first");
        }

        private void CheckKbiKibExtraction()
        {
            if (!areKbiKibExtracted) throw new InvalidOperationException(
                   "The boundary-remainder and internal-remainder submatrices (Kbi, Kib) must be calculated first");
        }

        private void CheckKccKcrKrcKrrExtraction()
        {
            if (!areKccKcrKrcKrrExtracted) throw new InvalidOperationException(
                   "The remainder and corner submatrices (Kcc, Kcr, Krc, Krr) must be calculated first.");
        }

        private void CheckKiiOrItsDiagonalInversion()
        {
            if (!isKiiOrItsDiagonalInverted) throw new InvalidOperationException(
                "The internal-remainder submatrix (Kii) or its diagonal must be inverted first");
        }

        private void CheckKrrAvailability()
        {
            if (!areKccKcrKrcKrrExtracted) throw new InvalidOperationException(
                    "The remainder submatrix (Krr) of must be calculated first.");
            if (isKrrOverwritten) throw new InvalidOperationException(
                    "The remainder submatrix (Krr) has been overwritten and cannot be used anymore."
                    + " Try calling this method before inverting it.");
        }

        private void CheckKrrInversion()
        {
            if (!isKrrInverted) throw new InvalidOperationException("The remainder submatrix (Krr) must be inverted first");
        }
    }
}
