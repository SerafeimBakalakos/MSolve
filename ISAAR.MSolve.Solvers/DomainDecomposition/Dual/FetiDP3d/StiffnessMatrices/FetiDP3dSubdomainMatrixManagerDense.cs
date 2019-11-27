using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.LinearAlgebra.Triangulation;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.Assemblers;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.Augmentation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.LinearSystems;
using ISAAR.MSolve.Solvers.Ordering.Reordering;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.StiffnessMatrices
{
    public class FetiDP3dSubdomainMatrixManagerDense : IFetiDP3dSubdomainMatrixManager
    {
        private readonly SkylineAssembler assembler = new SkylineAssembler();
        private readonly IAugmentationConstraints augmentationConstraints;
        private readonly IFetiDPDofSeparator dofSeparator;
        private readonly ILagrangeMultipliersEnumerator lagrangesEnumerator;
        private readonly SingleSubdomainSystem<SkylineMatrix> linearSystem;
        private readonly ISubdomain subdomain;

        private Vector fbc, fr, fcStar;
        private Matrix inverseKii;
        private DiagonalMatrix inverseKiiDiagonal;
        private CholeskyFull inverseKrr;
        private Matrix Kbb, Kbi;
        private Matrix Kcc, Krc, Krr;
        private Matrix _KccStar, _KacStar, _KaaStar;

        public FetiDP3dSubdomainMatrixManagerDense(ISubdomain subdomain, IFetiDPDofSeparator dofSeparator, 
            ILagrangeMultipliersEnumerator lagrangesEnumerator, IAugmentationConstraints augmentationConstraints)
        {
            this.subdomain = subdomain;
            this.dofSeparator = dofSeparator;
            this.lagrangesEnumerator = lagrangesEnumerator;
            this.augmentationConstraints = augmentationConstraints;
            this.linearSystem = new SingleSubdomainSystem<SkylineMatrix>(subdomain);
        }

        public IMatrixView KaaStar => _KaaStar;
        public IMatrixView KacStar => _KacStar;
        public IMatrixView KccStar => _KccStar;


        public ISingleSubdomainLinearSystem LinearSystem => linearSystem;

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
                return fcStar;
            }
        }

        public (IMatrix Kff, IMatrixView Kfc, IMatrixView Kcf, IMatrixView Kcc) BuildFreeConstrainedMatrices(
            ISubdomainFreeDofOrdering freeDofOrdering, ISubdomainConstrainedDofOrdering constrainedDofOrdering, 
            IEnumerable<IElement> elements, IElementMatrixProvider matrixProvider)
            => assembler.BuildGlobalSubmatrices(freeDofOrdering, constrainedDofOrdering, elements, matrixProvider);

        public void BuildFreeDofsMatrix(ISubdomainFreeDofOrdering dofOrdering, IElementMatrixProvider matrixProvider)
        {
            linearSystem.Matrix = assembler.BuildGlobalMatrix(dofOrdering,
                linearSystem.Subdomain.EnumerateElements(), matrixProvider);
        }

        public void CalcInverseKii(bool diagonalOnly)
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

        public void CalcSubdomainFcStartVector()
        {
            // fcStar[s] = fbc[s] - Krc[s]^T * inv(Krr[s]) * fr[s]
            Vector temp = MultiplyInverseKrrTimes(Fr);
            temp = MultiplyKcrTimes(temp);
            fcStar = Fbc - temp;
        }

        public void CalcSubdomainKStarMatrices()
        {
            // Top left
            // KccStar[s] = Kcc[s] - Krc[s]^T * inv(Krr[s]) * Krc[s]
            Matrix invKrrTimesKrc = inverseKrr.SolveLinearSystems(Krc);
            _KccStar = Kcc - Krc.MultiplyRight(invKrrTimesKrc, true);


            // Bottom right
            // KaaStar[s] = R1[s]^T * inv(Krr[s]) * R1[s]
            // R1[s] = Br[s]^T * Q1[s]
            SignedBooleanMatrixColMajor Br = lagrangesEnumerator.GetBooleanMatrix(subdomain);
            Matrix Q1 = augmentationConstraints.GetMatrixQ1(subdomain);
            Matrix R1 = Br.MultiplyRight(Q1, true);
            _KaaStar = R1.MultiplyRight(inverseKrr.SolveLinearSystems(R1), true); //TODO: This should be a method in boolean matrices

            // Bottom left
            // KacStar[s] = R1[s]^T * inv(Krr[s]) * Krc[s]
            _KacStar = R1.MultiplyRight(invKrrTimesKrc, true);

        }

        public void ClearMatrices()
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
            _KacStar = null;
            _KaaStar = null;
            //linearSystem.Matrix = null; // DO NOT DO THAT!!! The analyzer manages that.
        }

        public void ClearRhsVectors()
        {
            fbc = null;
            fr = null;
            fcStar = null;
        }

        public void ExtractCornerRemainderRhsSubvectors()
        {
            Vector Ff = LinearSystem.RhsConcrete;
            int[] cornerDofs = dofSeparator.GetCornerDofIndices(subdomain);
            int[] remainderDofs = dofSeparator.GetRemainderDofIndices(subdomain);
            fr = Ff.GetSubvector(remainderDofs);
            fbc = Ff.GetSubvector(cornerDofs);
            fcStar = null;
        }

        public void ExtractCornerRemainderSubmatrices()
        {
            int[] cornerDofs = dofSeparator.GetCornerDofIndices(subdomain);
            int[] remainderDofs = dofSeparator.GetRemainderDofIndices(subdomain);
            Kcc = linearSystem.Matrix.GetSubmatrixFull(cornerDofs, cornerDofs);
            Krc = linearSystem.Matrix.GetSubmatrixFull(remainderDofs, cornerDofs);
            Krr = linearSystem.Matrix.GetSubmatrixFull(remainderDofs, remainderDofs);
        }

        public void ExtractKbb()
        {
            int[] boundaryDofs = dofSeparator.GetBoundaryDofIndices(subdomain); 
            Kbb = Krr.GetSubmatrix(boundaryDofs, boundaryDofs);
        }

        public void ExtractKbiKib() 
        {
            int[] boundaryDofs = dofSeparator.GetBoundaryDofIndices(subdomain);
            int[] internalDofs = dofSeparator.GetInternalDofIndices(subdomain);
            Kbi = Krr.GetSubmatrix(boundaryDofs, internalDofs); 
        }

        public void HandleDofOrderingWillBeModified() => assembler.HandleDofOrderingWillBeModified();

        public void InvertKrr(bool inPlace) => inverseKrr = Krr.FactorCholesky(inPlace);

        public Vector MultiplyInverseKiiTimes(Vector vector, bool diagonalOnly)
            => diagonalOnly ? inverseKiiDiagonal * vector : inverseKii * vector;

        public Matrix MultiplyInverseKiiTimes(Matrix matrix, bool diagonalOnly)
        => diagonalOnly ? inverseKiiDiagonal * matrix : inverseKii * matrix;

        public Vector MultiplyInverseKrrTimes(Vector vector) => inverseKrr.SolveLinearSystem(vector);

        public Vector MultiplyKbbTimes(Vector vector) => Kbb * vector;

        public Matrix MultiplyKbbTimes(Matrix matrix) => Kbb * matrix;

        public Vector MultiplyKbiTimes(Vector vector) => Kbi * vector;

        public Matrix MultiplyKbiTimes(Matrix matrix) => Kbi * matrix;

        public Vector MultiplyKcrTimes(Vector vector) => Krc.Multiply(vector, true);

        public Vector MultiplyKibTimes(Vector vector) => Kbi.Multiply(vector, true);

        public Matrix MultiplyKibTimes(Matrix matrix) => Kbi.MultiplyRight(matrix, true);

        public Vector MultiplyKrcTimes(Vector vector) => Krc.Multiply(vector);

        public DofPermutation ReorderInternalDofs()
        {
            // Do nothing, since the sparsity pattern is irrelevant for dense matrices.
            return DofPermutation.CreateNoPermutation();
        }

        public DofPermutation ReorderRemainderDofs()
        {
            // Do nothing, since the sparsity pattern is irrelevant for dense matrices.
            return DofPermutation.CreateNoPermutation();
        }
    }
}
