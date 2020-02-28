using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.CornerNodes;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.InterfaceProblem;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.Augmentation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;
using ISAAR.MSolve.Solvers.Ordering.Reordering;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.StiffnessMatrices
{
    public class FetiDP3dGlobalMatrixManagerDense : IFetiDP3dGlobalMatrixManager
    {
        private readonly IAugmentationConstraints augmentationConstraints;
        private readonly IFetiDPDofSeparator dofSeparator;
        private readonly IModel model;

        private Vector globalFcStar;
        private bool hasInverseGlobalKccStarTilde;
        private Matrix inverseGlobalKccStarTilde;

        public FetiDP3dGlobalMatrixManagerDense(IModel model, IFetiDPDofSeparator dofSeparator,
            IAugmentationConstraints augmentationConstraints)
        {
            this.model = model;
            this.dofSeparator = dofSeparator;
            this.augmentationConstraints = augmentationConstraints;
        }

        public Vector CoarseProblemRhs
        {
            get
            {
                if (globalFcStar == null) throw new InvalidOperationException(
                    "The coarse problem RHS must be assembled from subdomains first.");
                return globalFcStar;
            }
        }

        public void CalcCoarseProblemRhs(Dictionary<ISubdomain, Vector> condensedRhsVectors)
        {
            // globalFcStar = sum_over_s(Bc[s]^T * fcStar[s])
            globalFcStar = Vector.CreateZero(dofSeparator.NumGlobalCornerDofs);
            foreach (ISubdomain subdomain in condensedRhsVectors.Keys)
            {
                UnsignedBooleanMatrix Bc = dofSeparator.GetCornerBooleanMatrix(subdomain);
                Vector fcStar = condensedRhsVectors[subdomain];
                globalFcStar.AddIntoThisNonContiguouslyFrom(Bc.GetRowsToColumnsMap(), fcStar);
            }
        }

        public void CalcInverseCoarseProblemMatrix(ICornerNodeSelection cornerNodeSelection,
            Dictionary<ISubdomain, (IMatrixView KccStar, IMatrixView KacStar, IMatrixView KaaStar)> matrices)
        {
            // globalKccStar = sum_over_s(Bc[s]^T * KccStar[s] * Bc[s])
            // globalKacStar = sum_over_s(Ba[s]^T * KacStar[s] * Bc[s])
            // globalKaaStar = sum_over_s(Ba[s]^T * KaaStar[s] * Ba[s])

            int numCorners = dofSeparator.NumGlobalCornerDofs;
            int numAugmented = augmentationConstraints.NumGlobalAugmentationConstraints;
            var globalKccStar = Matrix.CreateZero(numCorners, numCorners);
            var globalKacStar = Matrix.CreateZero(numAugmented, numCorners);
            var globalKaaStar = Matrix.CreateZero(numAugmented, numAugmented);
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                int s = subdomain.ID;
                (IMatrixView KccStar, IMatrixView KacStar, IMatrixView KaaStar) = matrices[subdomain];

                UnsignedBooleanMatrix Bc = dofSeparator.GetCornerBooleanMatrix(subdomain);
                GlobalToLocalBooleanMatrix Ba = augmentationConstraints.GetMatrixBa(subdomain);

                globalKccStar.AddIntoThis(Bc.ThisTransposeTimesOtherTimesThis(KccStar));
                globalKacStar.AddIntoThis(Ba.MultiplyRight(Bc.MultiplyLeft(KacStar), true));
                globalKaaStar.AddIntoThis(Ba.MultiplyThisTransposeTimesOtherTimesThis(KaaStar));
            }

            // KccTilde = [Kcc, Kac'; Kac Kaa];
            Matrix topRows = globalKccStar.AppendRight(globalKacStar.Transpose());
            Matrix bottomRows = globalKacStar.AppendRight(globalKaaStar);
            inverseGlobalKccStarTilde = topRows.AppendBottom(bottomRows);

            // Invert
            inverseGlobalKccStarTilde.InvertInPlace();
            hasInverseGlobalKccStarTilde = true;
        }

        public void CalcInverseCoarseProblemMatrix(ICornerNodeSelection cornerNodeSelection, 
            Dictionary<ISubdomain, IMatrixView> subdomainCoarseMatrices)
        {
            // globalKccStar = sum_over_s(Bc[s]^T * KccStar[s] * Bc[s])
            // globalKacStar = sum_over_s(Ba[s]^T * KacStar[s] * Bc[s])
            // globalKaaStar = sum_over_s(Ba[s]^T * KaaStar[s] * Ba[s])

            int numCorners = dofSeparator.NumGlobalCornerDofs;
            int numAugmented = augmentationConstraints.NumGlobalAugmentationConstraints;
            var globalKccStar = Matrix.CreateZero(numCorners, numCorners);
            var globalKacStar = Matrix.CreateZero(numAugmented, numCorners);
            var globalKaaStar = Matrix.CreateZero(numAugmented, numAugmented);
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                int s = subdomain.ID;

                // Split the matrix containing all coarse dofs of this subdomain to corner and augmented dofs
                int countC = dofSeparator.GetCornerDofIndices(subdomain).Length;
                int countA = augmentationConstraints.GetNumAugmentationDofs(subdomain);
                Matrix subdomainMatrix = subdomainCoarseMatrices[subdomain].CopyToFullMatrix();
                Matrix KccStar = subdomainMatrix.GetSubmatrix(0, countC, 0, countC);
                Matrix KaaStar = subdomainMatrix.GetSubmatrix(countC, countC + countA, countC, countC + countA);
                Matrix KacStar = subdomainMatrix.GetSubmatrix(countC, countC + countA, 0, countC);

                UnsignedBooleanMatrix Bc = dofSeparator.GetCornerBooleanMatrix(subdomain);
                GlobalToLocalBooleanMatrix Ba = augmentationConstraints.GetMatrixBa(subdomain);

                globalKccStar.AddIntoThis(Bc.ThisTransposeTimesOtherTimesThis(KccStar));
                globalKacStar.AddIntoThis(Ba.MultiplyRight(Bc.MultiplyLeft(KacStar), true));
                globalKaaStar.AddIntoThis(Ba.MultiplyThisTransposeTimesOtherTimesThis(KaaStar));
            }

            // KccTilde = [Kcc, Kac'; Kac Kaa];
            Matrix topRows = globalKccStar.AppendRight(globalKacStar.Transpose());
            Matrix bottomRows = globalKacStar.AppendRight(globalKaaStar);
            inverseGlobalKccStarTilde = topRows.AppendBottom(bottomRows);

            // Invert
            inverseGlobalKccStarTilde.InvertInPlace();
            hasInverseGlobalKccStarTilde = true;
        }

        public void ClearCoarseProblemRhs() => globalFcStar = null;

        public void ClearInverseCoarseProblemMatrix()
        {
            inverseGlobalKccStarTilde = null;
            hasInverseGlobalKccStarTilde = false;
        }

        public Vector MultiplyInverseCoarseProblemMatrixTimes(Vector vector)
        {
            if (!hasInverseGlobalKccStarTilde) throw new InvalidOperationException(
                "The inverse of the coarse problem matrix must be calculated first.");
            return inverseGlobalKccStarTilde * vector;
        }

        public DofPermutation ReorderCornerDofs() => DofPermutation.CreateNoPermutation();
    }
}
