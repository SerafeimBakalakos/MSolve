using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.LinearAlgebra.SchurComplements;
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
    public class FetiDP3dSubdomainMatrixManagerSkyline : IFetiDP3dSubdomainMatrixManager
    {
        private readonly SkylineAssembler assembler = new SkylineAssembler();
        private readonly IAugmentationConstraints augmentationConstraints;
        private readonly IFetiDPDofSeparator dofSeparator;
        private readonly ILagrangeMultipliersEnumerator lagrangesEnumerator;
        private readonly SingleSubdomainSystemMpi<SkylineMatrix> linearSystem;
        private readonly ISubdomain subdomain;

        private Vector fbc, fr, fcStar;
        private LdlSkyline inverseKii;
        private DiagonalMatrix inverseKiiDiagonal;
        private LdlSkyline inverseKrr;
        private Matrix Kbb;
        private CscMatrix Kib;
        private SymmetricMatrix Kcc;
        private CscMatrix Krc;
        private SkylineMatrix Krr;
        private SymmetricMatrix _KccStar, _KaaStar;
        private Matrix _KacStar;

        public FetiDP3dSubdomainMatrixManagerSkyline(ISubdomain subdomain, IFetiDPDofSeparator dofSeparator, 
            ILagrangeMultipliersEnumerator lagrangesEnumerator, IAugmentationConstraints augmentationConstraints)
        {
            this.subdomain = subdomain;
            this.dofSeparator = dofSeparator;
            this.lagrangesEnumerator = lagrangesEnumerator;
            this.augmentationConstraints = augmentationConstraints;
            this.linearSystem = new SingleSubdomainSystemMpi<SkylineMatrix>(subdomain);
        }

        public IMatrixView KaaStar => _KaaStar;
        public IMatrixView KacStar => _KacStar;
        public IMatrixView KccStar => _KccStar;


        public ISingleSubdomainLinearSystemMpi LinearSystem => linearSystem;

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
                SkylineMatrix Kii = Krr.GetSubmatrixSymmetricSkyline(internalDofs);
                inverseKii = Kii.FactorLdl(true);
            }
        }

        public void ClearMatrices()
        {
            inverseKii = null;
            inverseKiiDiagonal = null;
            inverseKrr = null;
            Kbb = null;
            Kib = null;
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

        public void CondenseMatricesStatically()
        {
            Start here:
            // Do inv(Krr[s]) * Krc[s] only once. Use it for the Schur complement of line 151
            // Then use it for the multiplication of line 167

            // Top left
            // KccStar[s] = Kcc[s] - Krc[s]^T * inv(Krr[s]) * Krc[s]
            _KccStar = SchurComplementCsc.CalcSchurComplementSymmetric(Kcc, Krc, inverseKrr);


            // Bottom right
            // KaaStar[s] = - Qr^T * Br[s] * inv(Krr[s]) * Br[s]^T * Qr <=>
            // KaaStar[s] = Ba[s]^T * (- R1[s]^T * inv(Krr[s]) * R1[s]) * Ba[s]
            // where Ba[s] is taken into account during assembly of the global coarse problem matrix
            var R1 = (LocalToGlobalMappingMatrix)augmentationConstraints.GetMatrixR1(subdomain);
            _KaaStar = R1.MultiplyTransposeThisTimesOtherTimesThis(inverseKrr);
            _KaaStar.ScaleIntoThis(-1);

            // Bottom left
            // KacStar[s] = - Qr^T * Br[s] * inv(Krr[s]) * Krc[s] <=>
            // KacStar[s] = - R1[s]^T * inv(Krr[s]) * Krc[s]
            // where Ba[s] is taken into account during assembly of the global coarse problem matrix
            _KacStar = R1.MultiplyRight(invKrrTimesKrc, true);
            _KacStar.ScaleIntoThis(-1);


            
        }

        public void CondenseRhsVectorsStatically()
        {
            // fcStar[s] = fbc[s] - Krc[s]^T * inv(Krr[s]) * fr[s]
            Vector temp = MultiplyInverseKrrTimes(Fr);
            temp = MultiplyKcrTimes(temp);
            fcStar = Fbc - temp;
        }

        public void ExtractBoundaryInternalSubmatricesAndInvertKii(bool diagonalKii)
        {
            int[] boundaryDofs = dofSeparator.GetBoundaryDofIndices(subdomain);
            int[] internalDofs = dofSeparator.GetInternalDofIndices(subdomain);

            Kbb = Krr.GetSubmatrixSymmetricFull(boundaryDofs);
            Kib = Krr.GetSubmatrixCsc(internalDofs, boundaryDofs);

            if (diagonalKii)
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
                SkylineMatrix Kii = Krr.GetSubmatrixSymmetricSkyline(internalDofs);
                inverseKii = Kii.FactorLdl(true);
            }
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
            Kcc = linearSystem.Matrix.GetSubmatrixSymmetricPacked(cornerDofs);
            Krc = linearSystem.Matrix.GetSubmatrixCsc(remainderDofs, cornerDofs);
            Krr = linearSystem.Matrix.GetSubmatrixSymmetricSkyline(remainderDofs);
        }

        public void ExtractKbb()
        {
            int[] boundaryDofs = dofSeparator.GetBoundaryDofIndices(subdomain);
            Kbb = Krr.GetSubmatrixSymmetricFull(boundaryDofs);
        }

        public void HandleDofOrderingWillBeModified() => assembler.HandleDofOrderingWillBeModified();

        public void InvertKrr(bool inPlace) => inverseKrr = Krr.FactorLdl(inPlace);

        public Vector MultiplyInverseKiiTimes(Vector vector, bool diagonalOnly)
            => diagonalOnly ? inverseKiiDiagonal * vector : inverseKii.SolveLinearSystem(vector);

        public Matrix MultiplyInverseKiiTimes(Matrix matrix, bool diagonalOnly)
        => diagonalOnly ? inverseKiiDiagonal * matrix : inverseKii.SolveLinearSystems(matrix);

        public Vector MultiplyInverseKrrTimes(Vector vector) => inverseKrr.SolveLinearSystem(vector);

        public Vector MultiplyKbbTimes(Vector vector) => Kbb * vector;

        public Matrix MultiplyKbbTimes(Matrix matrix) => Kbb * matrix;

        public Vector MultiplyKbiTimes(Vector vector) => Kib.Multiply(vector, true);

        public Matrix MultiplyKbiTimes(Matrix matrix) => Kib.MultiplyRight(matrix, true);

        public Vector MultiplyKccTimes(Vector vector) => Kcc * vector;

        public Vector MultiplyKcrTimes(Vector vector) => Krc.Multiply(vector, true);

        public Vector MultiplyKibTimes(Vector vector) => Kib.Multiply(vector);

        public Matrix MultiplyKibTimes(Matrix matrix) => Kib.MultiplyRight(matrix);

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
