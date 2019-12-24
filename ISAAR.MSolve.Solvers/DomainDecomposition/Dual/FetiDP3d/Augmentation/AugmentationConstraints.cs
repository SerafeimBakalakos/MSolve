using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.Augmentation
{
    public class AugmentationConstraints : IAugmentationConstraints
    {
        private readonly ILagrangeMultipliersEnumerator lagrangesEnumerator;
        private readonly IModel model;

        private Dictionary<ISubdomain, UnsignedBooleanMatrix> matricesBa = new Dictionary<ISubdomain, UnsignedBooleanMatrix>();
        private Dictionary<ISubdomain, Matrix> matricesR1 = new Dictionary<ISubdomain, Matrix>();

        public AugmentationConstraints(IModel model, IMidsideNodesSelection midsideNodesSelection,
            ILagrangeMultipliersEnumerator lagrangesEnumerator)
        {
            this.model = model;
            this.MidsideNodesSelection = midsideNodesSelection;
            this.lagrangesEnumerator = lagrangesEnumerator;
        }

        public UnsignedBooleanMatrix MatrixGlobalQr { get; private set; }
        public IMidsideNodesSelection MidsideNodesSelection { get; }

        public int NumGlobalAugmentationConstraints { get; private set; }

        public void CalcAugmentationMappingMatrices()
        {
            DofTable augmentedOrdering = OrderAugmentationConstraints();
            CalcGlobalMatrixQr(augmentedOrdering);
            CalcMatricesBa(augmentedOrdering);
            CalcMatricesR1();
        }

        public UnsignedBooleanMatrix GetMatrixBa(ISubdomain subdomain) => matricesBa[subdomain];

        public Matrix GetMatrixR1(ISubdomain subdomain) => matricesR1[subdomain];

        private void CalcGlobalMatrixQr(DofTable augmentedOrdering)
        {
            NumGlobalAugmentationConstraints = 
                MidsideNodesSelection.DofsPerNode.Length * MidsideNodesSelection.MidsideNodesGlobal.Count;
            MatrixGlobalQr = 
                new UnsignedBooleanMatrix(lagrangesEnumerator.NumLagrangeMultipliers, NumGlobalAugmentationConstraints);
            for (int i = 0; i < lagrangesEnumerator.NumLagrangeMultipliers; ++i)
            {
                LagrangeMultiplier lagr = lagrangesEnumerator.LagrangeMultipliers[i];
                bool isMidside = augmentedOrdering.TryGetValue(lagr.Node, lagr.DofType, out int augmentedIdx);
                if (isMidside) MatrixGlobalQr.AddEntry(i, augmentedIdx);
            }
        }

        private void CalcMatricesBa(DofTable augmentedOrdering)
        {
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                HashSet<INode> midsideNodes = MidsideNodesSelection.GetMidsideNodesOfSubdomain(subdomain);
                int numSubdomainAugmentedConstraints = midsideNodes.Count * MidsideNodesSelection.DofsPerNode.Length;
                var Ba = new UnsignedBooleanMatrix(numSubdomainAugmentedConstraints, NumGlobalAugmentationConstraints);
                int subdomainIdx = 0;
                foreach (INode node in midsideNodes)
                {
                    foreach (IDofType dof in MidsideNodesSelection.DofsPerNode)
                    {
                        int globalIdx = augmentedOrdering[node, dof];
                        Ba.AddEntry(subdomainIdx, globalIdx);
                        ++subdomainIdx;
                    }
                }
                matricesBa[subdomain] = Ba;
            }
        }

        private void CalcMatricesR1()
        {
            // Q1[s] = Qr * Ba[s]^T
            // R1[s] = Br[s]^T * Q1[s] 
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                SignedBooleanMatrixColMajor Br = lagrangesEnumerator.GetBooleanMatrix(subdomain);
                int[] columnsOfQrToKeep = matricesBa[subdomain].GetRowsToColumnsMap();
                Matrix Q1 = Matrix.CreateFromMatrix(MatrixGlobalQr.GetColumns(columnsOfQrToKeep));
                matricesR1[subdomain] = Br.MultiplyRight(Q1, true);
            }
        }

        private DofTable OrderAugmentationConstraints()
        {
            var ordering = new DofTable();
            int idx = 0;
            foreach (INode node in MidsideNodesSelection.MidsideNodesGlobal)
            {
                foreach (IDofType dof in MidsideNodesSelection.DofsPerNode) ordering[node, dof] = idx++;
            }
            return ordering;
        }

        public class Factory : IAugmentationConstraintsFactory
        {
            public IAugmentationConstraints CreateAugmentationConstraints(IModel model, IMidsideNodesSelection midsideNodesSelection,
                ILagrangeMultipliersEnumerator lagrangesEnumerator)
            {
                return new AugmentationConstraints(model, midsideNodesSelection, lagrangesEnumerator);
            }
        }
    }

}
