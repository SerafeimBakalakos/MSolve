using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.Augmentation
{
    public class AugmentationConstraints : IAugmentationConstraints
    {
        private readonly IDofType[] dofsPerNode;
        private readonly ILagrangeMultipliersEnumerator lagrangesEnumerator;
        private readonly IMidsideNodesSelection midsideNodesSelection;
        private readonly IModel model;

        public AugmentationConstraints(IModel model, IMidsideNodesSelection midsideNodesSelection, IDofType[] dofsPerNode,
            ILagrangeMultipliersEnumerator lagrangesEnumerator)
        {
            this.model = model;
            this.midsideNodesSelection = midsideNodesSelection;
            this.dofsPerNode = dofsPerNode;
            this.lagrangesEnumerator = lagrangesEnumerator;
        }

        public Dictionary<ISubdomain, Matrix> MatricesBa { get; } = new Dictionary<ISubdomain, Matrix>();

        public Dictionary<ISubdomain, Matrix> MatricesQ1 { get; } = new Dictionary<ISubdomain, Matrix>();

        public Matrix MatrixGlobalQr { get; private set; }

        public int NumGlobalAugmentationConstraints { get; private set; }

        public void CreateGlobalMatrixQr()
        {
            Table<INode, IDofType, HashSet<int>> augmentationLagranges =
                FindAugmentationLagranges(midsideNodesSelection, dofsPerNode, lagrangesEnumerator);
            NumGlobalAugmentationConstraints = dofsPerNode.Length * midsideNodesSelection.MidsideNodesGlobal.Count;
            MatrixGlobalQr = Matrix.CreateZero(lagrangesEnumerator.NumLagrangeMultipliers, NumGlobalAugmentationConstraints);

            for (int n = 0; n < midsideNodesSelection.MidsideNodesGlobal.Count; ++n)
            {
                INode node = midsideNodesSelection.MidsideNodesGlobal[n];
                int offset = n * dofsPerNode.Length;
                for (int j = 0; j < dofsPerNode.Length; ++j)
                {
                    HashSet<int> rowIndices = augmentationLagranges[node, dofsPerNode[j]];
                    foreach (int i in rowIndices) MatrixGlobalQr[i, offset + j] = 1.0;
                }
            }
        }

        //public void CreateSubdomainMappingMatrices()
        //{
        //    foreach (ISubdomain subdomain in model.EnumerateSubdomains())
        //    {
        //        (Matrix Ba, Matrix Q1) = CreateSubdomainMappingMatrices(subdomain);
        //        MatricesBa[subdomain] = Ba;
        //        MatricesQ1[subdomain] = Q1;
        //    }
        //}

        private void CreateSubdomainMappingMatrices(Table<INode, IDofType, HashSet<int>> augmentationLagranges)
        {
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                int numSubdomainAugmentedConstraints = 
                    midsideNodesSelection.GetMidsideNodesOfSubdomain(subdomain).Count * dofsPerNode.Length;
                MatricesBa[subdomain] = Matrix.CreateZero(numSubdomainAugmentedConstraints, NumGlobalAugmentationConstraints);
            }

            int globalOffset = 0;
            Dictionary<ISubdomain, int> subdomainOffsets = new Dictionary<ISubdomain, int>();
            for (int n = 0; n < midsideNodesSelection.MidsideNodesGlobal.Count; ++n)
            {
                INode node = midsideNodesSelection.MidsideNodesGlobal[n];
                foreach (IDofType dof in dofsPerNode)
                {
                    


                    HashSet<int> lagranges = augmentationLagranges[node, dof];
                    foreach (int i in lagranges)
                    {
                        LagrangeMultiplier lagr = lagrangesEnumerator.LagrangeMultipliers[i];
                        Matrix Ba = MatricesBa[lagr.SubdomainPlus];
                        Ba[subdomainOffsets[lagr.SubdomainPlus], globalOffset] = 1;
                    }
                    ++subdomainOffsets[lagr.SubdomainPlus];
                    ++globalOffset;
                }
                
            }

        }

        //TODO: This would be much faster if I used a Table<INode, IDofType, int> where int is the index of each lagrange 
        //multiplier in a vector with all lagrange multipliers (e.g. the solution of PCG).
        private static Table<INode, IDofType, HashSet<int>> FindAugmentationLagranges(
            IMidsideNodesSelection midsideNodesSelection, IEnumerable<IDofType> dofsPerNode, 
            ILagrangeMultipliersEnumerator lagrangesEnumerator)
        {
            var augmentationLagranges = new Table<INode, IDofType, HashSet<int>>();
            foreach (INode node in midsideNodesSelection.MidsideNodesGlobal)
            {
                foreach (IDofType dof in dofsPerNode) augmentationLagranges[node, dof] = new HashSet<int>();
            }

            var midsideNodes = new HashSet<INode>(midsideNodesSelection.MidsideNodesGlobal); // for faster look-ups. TODO: Use the table for look-ups
            for (int i = 0; i < lagrangesEnumerator.NumLagrangeMultipliers; ++i)
            {
                LagrangeMultiplier lagr = lagrangesEnumerator.LagrangeMultipliers[i];
                if (midsideNodes.Contains(lagr.Node))
                {
                    Debug.Assert(dofsPerNode.Contains(lagr.DofType));
                    augmentationLagranges[lagr.Node, lagr.DofType].Add(i);
                }
            }
            return augmentationLagranges;
        }
    }
}
