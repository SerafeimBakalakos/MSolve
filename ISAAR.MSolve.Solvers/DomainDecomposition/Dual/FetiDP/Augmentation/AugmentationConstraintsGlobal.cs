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

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.Augmentation
{
    public class AugmentationConstraintsGlobal : IAugmentationConstraints
    {
        public AugmentationConstraintsGlobal(IMidsideNodesSelection midsideNodesSelection, IDofType[] dofsPerNode,
            ILagrangeMultipliersEnumerator lagrangesEnumerator)
        {
            Table<INode, IDofType, HashSet<int>> augmentationLagranges = 
                FindAugmentationLagranges(midsideNodesSelection, dofsPerNode, lagrangesEnumerator);
            NumGlobalAugmentationConstraints = dofsPerNode.Length * midsideNodesSelection.MidsideNodesGlobal.Count;
            MatrixQr = Matrix.CreateZero(lagrangesEnumerator.NumLagrangeMultipliers, NumGlobalAugmentationConstraints);

            for (int n = 0; n < midsideNodesSelection.MidsideNodesGlobal.Count; ++n)
            {
                INode node = midsideNodesSelection.MidsideNodesGlobal[n];
                int offset = n * dofsPerNode.Length;
                for (int j = 0; j < dofsPerNode.Length; ++j)
                {
                    HashSet<int> rowIndices = augmentationLagranges[node, dofsPerNode[j]];
                    foreach (int i in rowIndices) MatrixQr[i, offset + j] = 1.0;
                }
            }
            
        }

        public Matrix MatrixQr { get; }

        public int NumGlobalAugmentationConstraints { get; }

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

            for (int i = 0; i < lagrangesEnumerator.NumLagrangeMultipliers; ++i)
            {
                LagrangeMultiplier lagr = lagrangesEnumerator.LagrangeMultipliers[i];
                if (midsideNodesSelection.MidsideNodesGlobal.Contains(lagr.Node))
                {
                    Debug.Assert(dofsPerNode.Contains(lagr.DofType));
                    augmentationLagranges[lagr.Node, lagr.DofType].Add(i);
                }
            }
            return augmentationLagranges;
        }
    }
}
