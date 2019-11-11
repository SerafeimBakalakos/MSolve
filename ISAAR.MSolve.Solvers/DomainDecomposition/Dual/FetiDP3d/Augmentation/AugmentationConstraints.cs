using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

        private Dictionary<ISubdomain, Matrix> matricesBa = new Dictionary<ISubdomain, Matrix>();
        private Dictionary<ISubdomain, Matrix> matricesQ1 = new Dictionary<ISubdomain, Matrix>();

        public AugmentationConstraints(IModel model, IMidsideNodesSelection midsideNodesSelection, IDofType[] dofsPerNode,
            ILagrangeMultipliersEnumerator lagrangesEnumerator)
        {
            this.model = model;
            this.midsideNodesSelection = midsideNodesSelection;
            this.dofsPerNode = dofsPerNode;
            this.lagrangesEnumerator = lagrangesEnumerator;
        }

        public Matrix MatrixGlobalQr { get; private set; }

        public int NumGlobalAugmentationConstraints { get; private set; }

        public void CalcAugmentationMappingMatrices()
        {
            DofTable augmentedOrdering = OrderAugmentationConstraints();
            CalcGlobalMatrixQr(augmentedOrdering);
            CalcMatricesBa(augmentedOrdering);
            CalcMatricesQ1();
        }

        public Matrix GetMatrixBa(ISubdomain subdomain) => matricesBa[subdomain];

        public Matrix GetMatrixQ1(ISubdomain subdomain) => matricesQ1[subdomain];

        private void CalcGlobalMatrixQr(DofTable augmentedOrdering)
        {
            NumGlobalAugmentationConstraints = dofsPerNode.Length * midsideNodesSelection.MidsideNodesGlobal.Count;
            MatrixGlobalQr = Matrix.CreateZero(lagrangesEnumerator.NumLagrangeMultipliers, NumGlobalAugmentationConstraints);
            for (int i = 0; i < lagrangesEnumerator.NumLagrangeMultipliers; ++i)
            {
                LagrangeMultiplier lagr = lagrangesEnumerator.LagrangeMultipliers[i];
                bool isMidside = augmentedOrdering.TryGetValue(lagr.Node, lagr.DofType, out int augmentedIdx);
                if (isMidside) MatrixGlobalQr[i, augmentedIdx] = 1;
            }
        }

        private void CalcMatricesBa(DofTable augmentedOrdering)
        {
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                HashSet<INode> midsideNodes = midsideNodesSelection.GetMidsideNodesOfSubdomain(subdomain);
                int numSubdomainAugmentedConstraints = midsideNodes.Count * dofsPerNode.Length;
                var Ba = Matrix.CreateZero(numSubdomainAugmentedConstraints, NumGlobalAugmentationConstraints);
                int subdomainIdx = 0;
                foreach (INode node in midsideNodes)
                {
                    foreach (IDofType dof in dofsPerNode)
                    {
                        int globalIdx = augmentedOrdering[node, dof];
                        Ba[subdomainIdx, globalIdx] = 1;
                        ++subdomainIdx;
                    }
                }
                matricesBa[subdomain] = Ba;
            }
        }

        private void CalcMatricesQ1()
        {
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                matricesQ1[subdomain] =  matricesBa[subdomain].MultiplyLeft(MatrixGlobalQr, true, false);
            }
        }

        private DofTable OrderAugmentationConstraints()
        {
            var ordering = new DofTable();
            int idx = 0;
            foreach (INode node in midsideNodesSelection.MidsideNodesGlobal)
            {
                foreach (IDofType dof in dofsPerNode) ordering[node, dof] = idx++;
            }
            return ordering;
        }
    }
}
