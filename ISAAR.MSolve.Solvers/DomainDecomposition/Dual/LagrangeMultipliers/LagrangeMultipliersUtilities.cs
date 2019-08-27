using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.Solvers.DomainDecomposition.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation;

//TODO: Rename the "remainder" in most method arguments. If anything it should be "internal". Also avoid passing the entry count.
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers
{
    /// <summary>
    /// Many of these methods overlap, because they have optimizations for serial / MPI environments.
    /// </summary>
    public static class LagrangeMultipliersUtilities
    {
        public static SignedBooleanMatrixColMajor CalcBooleanMatrix(ISubdomain subdomain, int numGlobalLagranges, 
            List<SubdomainLagrangeMultiplier> subdomainLagranges, int numRemainderDofs, DofTable remainderDofOrdering)
        {
            var booleanMatrix = new SignedBooleanMatrixColMajor(numGlobalLagranges, numRemainderDofs);
            foreach (SubdomainLagrangeMultiplier lagrange in subdomainLagranges)
            {
                int dofIdx = remainderDofOrdering[lagrange.Node, lagrange.DofType];
                booleanMatrix.AddEntry(lagrange.GlobalLagrangeIndex, dofIdx, lagrange.SubdomainSign);
            }
            return booleanMatrix;
        }

        public static SignedBooleanMatrixColMajor CalcBooleanMatrix(ISubdomain subdomain, LagrangeMultiplier[] globalLagranges,  
            int numRemainderDofs, DofTable remainderDofOrdering)
        {
            int numGlobalLagranges = globalLagranges.Length;
            var booleanMatrix = new SignedBooleanMatrixColMajor(numGlobalLagranges, numRemainderDofs);

            for (int i = 0; i < numGlobalLagranges; ++i) // Global lagrange multiplier index
            {
                LagrangeMultiplier lagrange = globalLagranges[i];
                if (lagrange.SubdomainPlus.ID == subdomain.ID)
                {
                    int dofIdx = remainderDofOrdering[lagrange.Node, lagrange.DofType];
                    booleanMatrix.AddEntry(i, dofIdx, true);
                }
                else if (lagrange.SubdomainMinus.ID == subdomain.ID)
                {
                    int dofIdx = remainderDofOrdering[lagrange.Node, lagrange.DofType];
                    booleanMatrix.AddEntry(i, dofIdx, true);
                }
            }

            return booleanMatrix;
        }

        //TODO: Not thrilled about having an array with missing or incomplete objects
        public static SignedBooleanMatrixColMajor CalcBooleanMatrixFromIncompleteData(ISubdomain subdomain,
            LagrangeMultiplier[] incompleteGlobalLagranges, int numRemainderDofs, DofTable remainderDofOrdering)
        {
            int numGlobalLagranges = incompleteGlobalLagranges.Length;
            var booleanMatrix = new SignedBooleanMatrixColMajor(numGlobalLagranges, numRemainderDofs);

            for (int i = 0; i < numGlobalLagranges; ++i) // Global lagrange multiplier index
            {
                LagrangeMultiplier lagrange = incompleteGlobalLagranges[i];
                if (lagrange == null) continue; // This lagrange's data is irrelevant and unavailable for the current subdomain.
                if ((lagrange.SubdomainPlus != null) && (lagrange.SubdomainPlus.ID == subdomain.ID))
                {
                    int dofIdx = remainderDofOrdering[lagrange.Node, lagrange.DofType];
                    booleanMatrix.AddEntry(i, dofIdx, true);
                }
                else if ((lagrange.SubdomainMinus != null) && (lagrange.SubdomainMinus.ID == subdomain.ID))
                {
                    int dofIdx = remainderDofOrdering[lagrange.Node, lagrange.DofType];
                    booleanMatrix.AddEntry(i, dofIdx, true);
                }
            }

            return booleanMatrix;
        }

        /// <summary>
        /// This method is slower than <see cref="CalcBooleanMatricesAndLagranges(IModel, int, 
        /// List{(INode node, IDofType[] dofs, ISubdomain[] subdomainsPlus, ISubdomain[] subdomainsMinus)}, Dictionary{int, int}, 
        /// Dictionary{int, DofTable})"/>. It probably does not matter that much though.
        /// </summary>
        public static Dictionary<int, SignedBooleanMatrixColMajor> CalcBooleanMatrices(LagrangeMultiplier[] globalLagranges,
            IModel model, Dictionary<int, int> numRemainderDofs, Dictionary<int, DofTable> remainderDofOrderings) 
        {
            //TODO: This is method is slower than CalcSignedBooleanMatricesAndLagranges(), the one that used 
            //      List<(INode node, IDofType[] dofs, ISubdomain[] subdomainsPlus, ISubdomain[] subdomainsMinus)>
            //      instead of a list of lagrange multipliers. It probably does not matter that much though.

            // Initialize the signed boolean matrices
            int numGlobalLagranges = globalLagranges.Length;
            var booleanMatrices = new Dictionary<int, SignedBooleanMatrixColMajor>();
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                booleanMatrices[subdomain.ID] =
                    new SignedBooleanMatrixColMajor(numGlobalLagranges, numRemainderDofs[subdomain.ID]);
            }

            // Fill the boolean matrices
            for (int i = 0; i < numGlobalLagranges; ++i) // Global lagrange multiplier index
            {
                LagrangeMultiplier lagrange = globalLagranges[i];
                int subdomainPlus = lagrange.SubdomainPlus.ID;
                int dofIdxPlus = remainderDofOrderings[subdomainPlus][lagrange.Node, lagrange.DofType];
                booleanMatrices[subdomainPlus].AddEntry(i, dofIdxPlus, true);

                int subdomainMinus = lagrange.SubdomainMinus.ID;
                int dofIdxMinus = remainderDofOrderings[subdomainMinus][lagrange.Node, lagrange.DofType];
                booleanMatrices[subdomainMinus].AddEntry(i, dofIdxMinus, false);
            }

            return booleanMatrices;
        }

        public static (Dictionary<int, SignedBooleanMatrixColMajor> booleanMatrices, LagrangeMultiplier[] lagranges)
            CalcBooleanMatricesAndLagranges(IModel model, int numLagrangeMultipliers,
            List<(INode node, IDofType[] dofs, ISubdomain[] subdomainsPlus, ISubdomain[] subdomainsMinus)> boundaryNodeData,
            Dictionary<int, int> numRemainderDofs, Dictionary<int, DofTable> remainderDofOrderings) //TODO: Rename the "remainder"
        {
            // Initialize the signed boolean matrices.
            var booleanMatrices = new Dictionary<int, SignedBooleanMatrixColMajor>();
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                booleanMatrices[subdomain.ID] =
                    new SignedBooleanMatrixColMajor(numLagrangeMultipliers, numRemainderDofs[subdomain.ID]);
            }
            var lagrangeMultipliers = new LagrangeMultiplier[numLagrangeMultipliers];

            // Fill the boolean matrices and lagrange multiplier data: node major, subdomain medium, dof minor. TODO: not sure about this order.
            int lag = 0; // Lagrange multiplier index
            foreach ((INode node, IDofType[] dofs, ISubdomain[] subdomainsPlus, ISubdomain[] subdomainsMinus)
                in boundaryNodeData)
            {
                int numSubdomainCombos = subdomainsPlus.Length;
                for (int c = 0; c < numSubdomainCombos; ++c)
                {
                    //TODO: each subdomain appears in many combinations. It would be faster to cache its indices for the specific dofs.
                    SignedBooleanMatrixColMajor booleanPlus = booleanMatrices[subdomainsPlus[c].ID];
                    SignedBooleanMatrixColMajor booleanMinus = booleanMatrices[subdomainsMinus[c].ID];

                    //TODO: The dof indices have already been accessed. Reuse it if possible.
                    IReadOnlyDictionary<IDofType, int> dofsPlus = remainderDofOrderings[subdomainsPlus[c].ID].GetDataOfRow(node);
                    IReadOnlyDictionary<IDofType, int> dofsMinus = remainderDofOrderings[subdomainsMinus[c].ID].GetDataOfRow(node);

                    foreach (IDofType dof in dofs)
                    {
                        booleanPlus.AddEntry(lag, dofsPlus[dof], true);
                        booleanMinus.AddEntry(lag, dofsMinus[dof], false);
                        lagrangeMultipliers[lag] = new LagrangeMultiplier(node, dof, subdomainsPlus[c], subdomainsMinus[c]);
                        ++lag;
                    }
                }
            }

            return (booleanMatrices, lagrangeMultipliers);
        }

        public static (List<(INode node, IDofType[] dofs, ISubdomain[] subdomainsPlus, ISubdomain[] subdomainsMinus)> combos,
            int numLagranges) DefineLagrangeCombinations(IDofSeparator dofSeparator, ICrosspointStrategy crosspointStrategy)
        {
            // Find boundary dual nodes and dofs
            var boundaryNodeData = new List<(INode node, IDofType[] dofs, ISubdomain[] subdomainsPlus,
                ISubdomain[] subdomainsMinus)>(dofSeparator.GlobalBoundaryDofs.Count);

            // Find continuity equations.
            int numLagrangeMultipliers = 0;
            foreach (var nodeDofsPair in dofSeparator.GlobalBoundaryDofs)
            {
                INode node = nodeDofsPair.Key;
                IDofType[] dofsOfNode = nodeDofsPair.Value;
                ISubdomain[] nodeSubdomains = node.SubdomainsDictionary.Values.ToArray();
                ISubdomain[] subdomainsPlus, subdomainsMinus;
                int multiplicity = nodeSubdomains.Length;
                if (multiplicity == 2)
                {
                    subdomainsPlus = new ISubdomain[] { nodeSubdomains[0] };
                    subdomainsMinus = new ISubdomain[] { nodeSubdomains[1] };
                }
                else (subdomainsPlus, subdomainsMinus) = crosspointStrategy.FindSubdomainCombinations(nodeSubdomains);

                boundaryNodeData.Add((node, dofsOfNode, subdomainsPlus, subdomainsMinus));
                numLagrangeMultipliers += dofsOfNode.Length * subdomainsPlus.Length;
            }

            return (boundaryNodeData, numLagrangeMultipliers);
        }

        public static List<LagrangeMultiplier> DefineLagrangeMultipliers(IFetiDPDofSeparator dofSeparator,
            ICrosspointStrategy crosspointStrategy)
        {
            var lagranges = new List<LagrangeMultiplier>();
            foreach (var nodeDofsPair in dofSeparator.GlobalBoundaryDofs)
            {
                INode node = nodeDofsPair.Key;
                IDofType[] dofsOfNode = nodeDofsPair.Value;
                ISubdomain[] nodeSubdomains = node.SubdomainsDictionary.Values.ToArray();

                // Find the how many lagrange multipliers are needed for each dof of this node 
                // and between which subdomains they should be applied to 
                ISubdomain[] subdomainsPlus, subdomainsMinus;
                int multiplicity = nodeSubdomains.Length;
                if (multiplicity == 2)
                {
                    subdomainsPlus = new ISubdomain[] { nodeSubdomains[0] };
                    subdomainsMinus = new ISubdomain[] { nodeSubdomains[1] };
                }
                else (subdomainsPlus, subdomainsMinus) = crosspointStrategy.FindSubdomainCombinations(nodeSubdomains);

                // Add the lagrange multipliers of this node to the global list. The order is important: 
                // node major - subdomain combination medium - dof minor.
                int numSubdomainCombos = subdomainsPlus.Length;
                for (int c = 0; c < numSubdomainCombos; ++c)
                {
                    foreach (IDofType dof in dofsOfNode)
                    {
                        lagranges.Add(new LagrangeMultiplier(node, dof, subdomainsPlus[c], subdomainsMinus[c]));
                    }
                }
            }
            return lagranges;
        }
    }
}
