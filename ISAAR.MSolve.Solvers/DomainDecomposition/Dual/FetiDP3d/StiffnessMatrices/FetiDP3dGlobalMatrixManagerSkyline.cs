using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Builders;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.LinearAlgebra.Output;
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

        // The next three ordering fields should be managed correctly and possibly by a dedicated class
        private Dictionary<ISubdomain, int[]> coarseDofMapsSubdomainToGlobal;
        private DofTable coarseDofOrdering;
        /// <summary>
        /// New-to-old
        /// </summary>
        private int[] coarseDofPermutation;

        private Vector globalFcStar;
        private bool hasInverseGlobalKccStarTilde;
        private LdlSkyline inverseGlobalKccStarTilde;

        public FetiDP3dGlobalMatrixManagerSkyline(IModel model, IFetiDPDofSeparator dofSeparator,
            IAugmentationConstraints augmentationConstraints, IReorderingAlgorithm reordering)
        {
            this.model = model;
            this.dofSeparator = dofSeparator;
            this.augmentationConstraints = augmentationConstraints;

            if (reordering == null) throw new NotImplementedException();
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
            OrderCoarseProblemDofs();

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

                // Subdomain-to-global mapping array
                int[] subToGlobalIndices = coarseDofMapsSubdomainToGlobal[subdomain];

                //skylineBuilder.AddSubmatrixSymmetric(KccStar, subToGlobalCornerIndices);
                //skylineBuilder.AddSubmatrixSymmetric(KaaStar, subToGlobalAugmentationIndices);
                //skylineBuilder.AddSubmatrixToLowerTriangle(KacStar, subToGlobalAugmentationIndices, subToGlobalCornerIndices);

                #region debug
                Matrix temp = KacStar.CopyToFullMatrix();
                Matrix matrix = KccStar.CopyToFullMatrix().AppendRight(temp.Transpose());
                temp = temp.AppendRight(KaaStar.CopyToFullMatrix());
                matrix = matrix.AppendBottom(temp);

                skylineBuilder.AddSubmatrixSymmetric(matrix, subToGlobalIndices);
                #endregion
            }
            SkylineMatrix globalKccStarTilde = skylineBuilder.BuildSkylineMatrix();

            // Factorization
            this.inverseGlobalKccStarTilde = globalKccStarTilde.FactorLdl(true);
            hasInverseGlobalKccStarTilde = true;
        }

        public void CalcInverseCoarseProblemMatrix(ICornerNodeSelection cornerNodeSelection, 
            Dictionary<ISubdomain, IMatrixView> subdomainCoarseMatrices)
        {
            OrderCoarseProblemDofs();

            // globalKccStar = sum_over_s(Bc[s]^T * KccStar[s] * Bc[s])
            // globalKacStar = sum_over_s(Ba[s]^T * KacStar[s] * Bc[s])
            // globalKaaStar = sum_over_s(Ba[s]^T * KaaStar[s] * Ba[s])
            // However K matrices are joined as [KccStar, KacStar^T; KacStar, KaaStar] and Bc, Ba matrices are no longer very
            // useful, as the mappings they represent have been permuted.

            // Assembly
            int numCornerDofs = dofSeparator.NumGlobalCornerDofs;
            int numAugmentationDofs = augmentationConstraints.NumGlobalAugmentationConstraints;
            int numCoarseDofs = numCornerDofs + numAugmentationDofs;
            int[] skylineColHeights =
                FindSkylineColumnHeights(cornerNodeSelection, augmentationConstraints.MidsideNodesSelection);
            var skylineBuilder = SkylineBuilder.Create(numCoarseDofs, skylineColHeights);
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                int[] subToGlobalIndices = coarseDofMapsSubdomainToGlobal[subdomain]; // subdomain-to-global mapping array
                IMatrixView subdomainMatrix = subdomainCoarseMatrices[subdomain]; // corner dofs followed by augmentation dofs
                skylineBuilder.AddSubmatrixSymmetric(subdomainMatrix, subToGlobalIndices);
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

            if (coarseDofPermutation != null)
            {
                //TODO: These should be handled by a dedicated PermutationMatrix class
                Vector permutedInputVector = vector.Reorder(coarseDofPermutation, false);
                Vector permutedOutputVector = inverseGlobalKccStarTilde.SolveLinearSystem(permutedInputVector);
                Vector outputVector = permutedOutputVector.Reorder(coarseDofPermutation, true);

                return outputVector;
            }
            else return inverseGlobalKccStarTilde.SolveLinearSystem(vector);
        }

        public DofPermutation ReorderCornerDofs()
        {
            coarseDofOrdering = null;
            coarseDofPermutation = null;
            coarseDofMapsSubdomainToGlobal = null;

            // For outside code the ordering remains the same
            return DofPermutation.CreateNoPermutation();
        }

        //TODO: Duplication between this and the 2D version
        private int[] FindSkylineColumnHeights(ICornerNodeSelection cornerNodeSelection, 
            IMidsideNodesSelection midsideNodesSelection)
        {
            int numCornerDofs = dofSeparator.NumGlobalCornerDofs;
            int numCoarseDofs = numCornerDofs + augmentationConstraints.NumGlobalAugmentationConstraints;

            // Only entries above the diagonal count towards the column height
            int[] colHeights = new int[numCoarseDofs];
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                // Put all coarse problem nodes (corners and midsides) together.
                var coarseNodes = new HashSet<INode>(cornerNodeSelection.GetCornerNodesOfSubdomain(subdomain));
                coarseNodes.UnionWith(midsideNodesSelection.GetMidsideNodesOfSubdomain(subdomain));

                // To determine the col height, first find the min of the dofs of this element. All these are 
                // considered to interact with each other, even if there are 0.0 entries in the element stiffness matrix.
                int minDof = int.MaxValue;
                foreach (INode node in coarseNodes)
                {
                    foreach (int dof in coarseDofOrdering.GetValuesOfRow(node)) minDof = Math.Min(dof, minDof);
                }

                // The height of each col is updated for all elements that engage the corresponding dof. 
                // The max height is stored.
                foreach (INode node in coarseNodes)
                {
                    foreach (int dof in coarseDofOrdering.GetValuesOfRow(node))
                    {
                        colHeights[dof] = Math.Max(colHeights[dof], dof - minDof);
                    }
                }
            }
            return colHeights;
        }

        private void MapCoarseDofsSubdomainToGlobal()
        {
            //TODO: This will be faster if I used the mappings defined by Bc, Ba and the coarse dof permutation array.
            this.coarseDofMapsSubdomainToGlobal = new Dictionary<ISubdomain, int[]>();
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                int numCornerDofs = dofSeparator.GetCornerDofIndices(subdomain).Length;
                int numAugmentationDofs = augmentationConstraints.GetNumAugmentationDofs(subdomain);
                var subToGlobalIndices = new int[numCornerDofs + numAugmentationDofs];
                coarseDofMapsSubdomainToGlobal[subdomain] = subToGlobalIndices;

                DofTable cornerDofOrdering = dofSeparator.GetCornerDofOrdering(subdomain);
                foreach ((INode node, IDofType dof, int localIdx) in cornerDofOrdering)
                {
                    subToGlobalIndices[localIdx] = coarseDofOrdering[node, dof];
                }

                DofTable augmentationDofOrdering = augmentationConstraints.GetAugmentationDofOrdering(subdomain);
                foreach ((INode node, IDofType dof, int localIdx) in augmentationDofOrdering)
                {
                    subToGlobalIndices[numCornerDofs + localIdx] = coarseDofOrdering[node, dof];
                }
            }
        }

        private void OrderCoarseProblemDofs()
        {
            if (coarseDofOrdering != null) return; //TODO: Needs thoughtful state management for this and the permutation

            // Find a naive global dof ordering of coarse dofs, where all corner dofs are numbered before all augmentation dofs
            int numCornerDofs = dofSeparator.NumGlobalCornerDofs;
            this.coarseDofOrdering = dofSeparator.GlobalCornerDofOrdering.DeepCopy();
            foreach ((INode node, IDofType dof, int idx) in augmentationConstraints.GlobalAugmentationDofOrdering)
            { // TODO: Do this in DofTable
                coarseDofOrdering[node, dof] = numCornerDofs + idx;
            }

            // Reorder the coarse problem dofs
            (int[] permutation, bool oldToNew) = ReorderCoarseProblemDofs(coarseDofOrdering);
            if (oldToNew) throw new NotImplementedException();
            #region debug
            //permutation = Enumerable.Range(0, permutation.Length).ToArray();
            #endregion
            this.coarseDofOrdering.Reorder(permutation, oldToNew);
            this.coarseDofPermutation = permutation;

            // Create subdomain to coarse problem (global) dof mappings so that they can be used (multiple times) 
            // during matrix assembly
            MapCoarseDofsSubdomainToGlobal();
        }

        private (int[] permutation, bool oldToNew) ReorderCoarseProblemDofs(DofTable coarseDofOrdering)
        {
            // Find the sparsity pattern for this ordering
            int numCoarseProblemDofs = 
                dofSeparator.NumGlobalCornerDofs + augmentationConstraints.NumGlobalAugmentationConstraints;
            var pattern = SparsityPatternSymmetric.CreateEmpty(numCoarseProblemDofs);
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                // Treat each subdomain as a superelement with only its corner and midside nodes.
                int numSubdomainCornerDofs = dofSeparator.GetCornerDofIndices(subdomain).Length;
                int numSubdomainAugmentationDofs = augmentationConstraints.GetNumAugmentationDofs(subdomain);
                var subdomainToGlobalDofs = new int[numSubdomainCornerDofs + numSubdomainAugmentationDofs];

                // Corner dofs
                DofTable subdomainCornerDofOrdering = dofSeparator.GetCornerDofOrdering(subdomain);
                foreach ((INode node, IDofType dofType, int localIdx) in subdomainCornerDofOrdering)
                {
                    int globalIdx = coarseDofOrdering[node, dofType];
                    subdomainToGlobalDofs[localIdx] = globalIdx;
                }

                // Augmentation dofs follow corner dofs
                DofTable subdomainAugmentationDofOrdering = augmentationConstraints.GetAugmentationDofOrdering(subdomain);
                foreach ((INode node, IDofType dofType, int localIdx) in subdomainAugmentationDofOrdering)
                {
                    int globalIdx = coarseDofOrdering[node, dofType];
                    subdomainToGlobalDofs[localIdx + numSubdomainCornerDofs] = globalIdx;
                }

                pattern.ConnectIndices(subdomainToGlobalDofs, false);
            }

            // Reorder the coarse dofs
            return reordering.FindPermutation(pattern);
        }
    }
}
