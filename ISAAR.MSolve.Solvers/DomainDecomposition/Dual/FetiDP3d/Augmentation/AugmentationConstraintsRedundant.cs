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

//TODO: This creates larger coarse problems and more MPI communication per PCG iteration. However the PCG iterations are fewer!!!
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.Augmentation
{
    public class AugmentationConstraintsRedundant : IAugmentationConstraints
    {
        public AugmentationConstraintsRedundant(IMidsideNodesSelection midsideNodesSelection, IDofType[] dofsPerNode,
            ILagrangeMultipliersEnumerator lagrangesEnumerator)
        {
            Table<INode, IDofType, HashSet<int>> augmentationLagranges =
                FindAugmentationLagranges(midsideNodesSelection, dofsPerNode, lagrangesEnumerator);
            NumGlobalAugmentationConstraints = 0;
            foreach ((INode node, IDofType dof, HashSet<int> val) in augmentationLagranges)
            {
                NumGlobalAugmentationConstraints += val.Count;
            }
            MatrixGlobalQr = Matrix.CreateZero(lagrangesEnumerator.NumLagrangeMultipliers, NumGlobalAugmentationConstraints);

            int col = 0;
            foreach (INode node in midsideNodesSelection.MidsideNodesGlobal)
            {
                foreach (IDofType dof in dofsPerNode)
                {
                    foreach (int idx in augmentationLagranges[node, dof])
                    {
                        MatrixGlobalQr[idx, col] = 1.0;
                        ++col;
                    }
                }
            }
        }

        public Matrix MatrixGlobalQr { get; }

        public int NumGlobalAugmentationConstraints { get; }

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

        //private static Dictionary<INode, List<int>> FindAugmentationLagranges(IMidsideNodesSelection midsideNodesSelection,
        //    ILagrangeMultipliersEnumerator lagrangesEnumerator)
        //{
        //    var augmentationLagranges = new Dictionary<INode, List<int>>();
        //    foreach (INode node in midsideNodesSelection.MidsideNodesGlobal) augmentationLagranges[node] = new List<int>();

        //    for (int i = 0; i < lagrangesEnumerator.NumLagrangeMultipliers; ++i)
        //    {
        //        LagrangeMultiplier lagr = lagrangesEnumerator.LagrangeMultipliers[i];
        //        if (augmentationLagranges.ContainsKey(lagr.Node))
        //        {
        //            if (lagr.Node.ID == 38)
        //            {
        //                Console.WriteLine();
        //            }

        //            augmentationLagranges[lagr.Node].Add(i);
        //        }
        //    }
        //    return augmentationLagranges;
        //}
    }
}
