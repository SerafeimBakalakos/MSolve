using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Builders;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.LinearAlgebra.Reordering;
using ISAAR.MSolve.LinearAlgebra.Triangulation;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.CornerNodes;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.Augmentation;
using ISAAR.MSolve.Solvers.Ordering.Reordering;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.StiffnessMatrices
{
    public class FetiDP3dGlobalMatrixManagerSkyline : IFetiDP3dGlobalMatrixManager
    {
        private readonly IAugmentationConstraints augmentationConstraints;
        private readonly IFetiDPDofSeparator dofSeparator;
        private readonly IModel model;
        private readonly IReorderingAlgorithm reordering;

        private Vector globalFcStar;
        private bool hasInverseGlobalKccStarTilde;
        private LdlSkyline inverseGlobalKccStarTilde;

        public FetiDP3dGlobalMatrixManagerSkyline(IModel model, IFetiDPDofSeparator dofSeparator,
            IAugmentationConstraints augmentationConstraints, IReorderingAlgorithm reordering)
        {
            this.model = model;
            this.dofSeparator = dofSeparator;
            this.augmentationConstraints = augmentationConstraints;
            this.reordering = reordering;
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


            // Assembly
            int numCornerDofs = dofSeparator.NumGlobalCornerDofs;
            int numAugmentationDofs = augmentationConstraints.NumGlobalAugmentationConstraints;
            int numCoarseDofs = numCornerDofs + numAugmentationDofs;
            int[] skylineColHeights = 
                FindSkylineColumnHeights(cornerNodeSelection, augmentationConstraints.MidsideNodesSelection);
            var skylineBuilder = SkylineBuilder.Create(numCoarseDofs, skylineColHeights);
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                (IMatrixView KccStar, IMatrixView KacStar, IMatrixView KaaStar) = matrices[subdomain];

                // Subdomain-to-global mapping arrays. Augmentation dofs follow all corner dofs.
                int[] subToGlobalCornerIndices = dofSeparator.GetCornerBooleanMatrix(subdomain).GetRowsToColumnsMap();
                int[] subToGlobalAugmentationIndices = augmentationConstraints.GetMatrixBa(subdomain).GetRowsToColumnsMap();
                for (int i = 0; i < subToGlobalAugmentationIndices.Length; ++i)
                {
                    subToGlobalAugmentationIndices[i] += numCornerDofs;
                }
                

                skylineBuilder.AddSubmatrixSymmetric(KccStar, subToGlobalCornerIndices);
                skylineBuilder.AddSubmatrixSymmetric(KaaStar, subToGlobalAugmentationIndices);
                skylineBuilder.AddSubmatrixToLowerTriangle(KacStar, subToGlobalAugmentationIndices, subToGlobalCornerIndices);
            }
            SkylineMatrix globalKccStarTilde = skylineBuilder.BuildSkylineMatrix();

            // Factorization
            this.inverseGlobalKccStarTilde = globalKccStarTilde.FactorLdl(true);
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
            return inverseGlobalKccStarTilde.SolveLinearSystem(vector);
        }

        public DofPermutation ReorderCoarseProblemDofs() => DofPermutation.CreateNoPermutation();

        private DofPermutation ReorderGlobalCornerDofs()
        {
            if (reordering == null) throw new ArgumentException();

            // Find global dof ordering of coarse dofs
            int numCornerDofs = dofSeparator.NumGlobalCornerDofs;
            int numCoarseDofs = numCornerDofs + augmentationConstraints.NumGlobalAugmentationConstraints;

            //TODO: Do this in another method and store the result after reordering
            var coarseDofOrdering = dofSeparator.GlobalCornerDofOrdering.CopyShallow();
            // TODO: Do this in DofTable
            foreach ((INode node, IDofType dof, int idx) in augmentationConstraints.GlobalAugmentationDofOrdering)
            {
                coarseDofOrdering[node, dof] = numCornerDofs + idx;
            }

            var pattern = SparsityPatternSymmetric.CreateEmpty(dofSeparator.NumGlobalCornerDofs);
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                // Treat each subdomain as a superelement with only its corner nodes.
                DofTable localCornerDofOrdering = dofSeparator.GetCornerDofOrdering(subdomain);
                int numLocalCornerDofs = localCornerDofOrdering.EntryCount;
                var subdomainToGlobalDofs = new int[numLocalCornerDofs];
                foreach ((INode node, IDofType dofType, int localIdx) in localCornerDofOrdering)
                {
                    int globalIdx = coarseDofOrdering[node, dofType];
                    subdomainToGlobalDofs[localIdx] = globalIdx;
                }
                pattern.ConnectIndices(subdomainToGlobalDofs, false);
            }
            (int[] permutation, bool oldToNew) = reordering.FindPermutation(pattern);
            return DofPermutation.Create(permutation, oldToNew);
        }

        //TODO: Duplication between this and the 2D version
        private int[] FindSkylineColumnHeights(ICornerNodeSelection cornerNodeSelection, 
            IMidsideNodesSelection midsideNodesSelection)
        {
            // Find global dof ordering of coarse dofs
            int numCornerDofs = dofSeparator.NumGlobalCornerDofs;
            var coarseDofOrdering = dofSeparator.GlobalCornerDofOrdering.CopyShallow();
            // TODO: Do this in DofTable
            foreach ((INode node, IDofType dof, int idx) in augmentationConstraints.GlobalAugmentationDofOrdering)
            {
                coarseDofOrdering[node, dof] = numCornerDofs + idx;
            }

            // Only entries above the diagonal count towards the column height
            int numCoarseDofs = numCornerDofs + augmentationConstraints.NumGlobalAugmentationConstraints;
            int[] colHeights = new int[numCoarseDofs];
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                // Put all coarse problem dofs (and their ordering) together
                var coarseDofs = new HashSet<INode>(cornerNodeSelection.GetCornerNodesOfSubdomain(subdomain));
                coarseDofs.UnionWith(midsideNodesSelection.GetMidsideNodesOfSubdomain(subdomain));

                // To determine the col height, first find the min of the dofs of this element. All these are 
                // considered to interact with each other, even if there are 0.0 entries in the element stiffness matrix.
                int minDof = int.MaxValue;
                foreach (INode node in coarseDofs)
                {
                    foreach (int dof in coarseDofOrdering.GetValuesOfRow(node)) minDof = Math.Min(dof, minDof);
                }

                // The height of each col is updated for all elements that engage the corresponding dof. 
                // The max height is stored.
                foreach (INode node in coarseDofs)
                {
                    foreach (int dof in coarseDofOrdering.GetValuesOfRow(node))
                    {
                        colHeights[dof] = Math.Max(colHeights[dof], dof - minDof);
                    }
                }
            }
            return colHeights;
        }
    }
}
