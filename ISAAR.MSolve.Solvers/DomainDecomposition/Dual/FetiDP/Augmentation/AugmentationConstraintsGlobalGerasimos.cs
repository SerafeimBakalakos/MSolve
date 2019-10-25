using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.Augmentation
{
    public class AugmentationConstraintsGlobalGerasimos : IAugmentationConstraints
    {
        public AugmentationConstraintsGlobalGerasimos(IMidsideNodesSelection midsideNodesSelection,
            ILagrangeMultipliersEnumerator lagrangesEnumerator)
        {
            HashSet<int> augmentationLagranges = FindAugmentationLagranges(midsideNodesSelection, lagrangesEnumerator);
            NumGlobalAugmentationConstraints = augmentationLagranges.Count;
            MatrixQr = Matrix.CreateZero(lagrangesEnumerator.NumLagrangeMultipliers, NumGlobalAugmentationConstraints);

            int col = 0;
            foreach (int idx in augmentationLagranges)
            {
                MatrixQr[idx, col] = 1.0;
                ++col;
            }
        }

        public Matrix MatrixQr { get; }

        public int NumGlobalAugmentationConstraints { get; }

        private static HashSet<int> FindAugmentationLagranges(IMidsideNodesSelection midsideNodesSelection,
            ILagrangeMultipliersEnumerator lagrangesEnumerator)
        {
            var augmentationLagranges = new HashSet<int>();
            for (int i = 0; i < lagrangesEnumerator.NumLagrangeMultipliers; ++i)
            {
                LagrangeMultiplier lagr = lagrangesEnumerator.LagrangeMultipliers[i];
                if (midsideNodesSelection.MidsideNodesGlobal.Contains(lagr.Node))
                {
                    augmentationLagranges.Add(i);
                }
            }
            return augmentationLagranges;
        }
    }
}
