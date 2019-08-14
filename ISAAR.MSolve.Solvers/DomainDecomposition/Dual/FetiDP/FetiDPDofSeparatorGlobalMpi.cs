using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.Solvers.DomainDecomposition.DofSeparation;

namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP
{
    public class FetiDPDofSeparatorGlobalMpi
    {
        private readonly GlobalFreeDofOrderingMpi globalDofOrdering; //TODO: This should be accessed by IModelMpi instead of being injected in the constructor

        public FetiDPDofSeparatorGlobalMpi(IStructuralModel model, GlobalFreeDofOrderingMpi globalDofOrdering)
        {
            this.Model = model;
            this.globalDofOrdering = globalDofOrdering;
            //BoundaryDofs = new Dictionary<int, (INode node, IDofType dofType)[]>();
            CornerBooleanMatrices = new Dictionary<int, UnsignedBooleanMatrix>();
            SubdomainCornerDofOrderings = new Dictionary<int, DofTable>();
        }

        ///// <summary>
        ///// (Node, IDofType) pairs for each boundary remainder dof of each subdomain. Their order is the same one as 
        ///// <see cref="BoundaryDofIndices"/>.
        ///// </summary>
        //public Dictionary<int, (INode node, IDofType dofType)[]> BoundaryDofs { get; private set; } //These are stored both in the process of each subdomain and in the master process.

        /// <summary>
        /// Also called Bc in papers by Farhat or Lc in NTUA theses. 
        /// </summary>
        public Dictionary<int, UnsignedBooleanMatrix> CornerBooleanMatrices { get; } // Master creates, stores and scatters them. They are needed per process and globally.

        /// <summary>
        /// Dofs where Lagrange multipliers will be applied. These do not include corner dofs.
        /// </summary>
        public Dictionary<INode, IDofType[]> GlobalBoundaryDofs { get; private set; }

        /// <summary>
        /// Dof ordering for corner dofs of the model: Each (INode, IDofType) pair is associated with the index of that dof into 
        /// a vector corresponding to all corner dofs of the model.
        /// </summary>
        public DofTable GlobalCornerDofOrdering { get; private set; } // Only for master

        /// <summary>
        /// If Xf is a vector with all free dofs of the model and Xc is a vector with all corner dofs of the model, then
        /// Xf[GlobalCornerToFreeDofMap[i]] = Xc[i].
        /// </summary>
        public int[] GlobalCornerToFreeDofMap { get; set; } // Only for master

        public IStructuralModel Model {get;}

        /// <summary>
        /// The number of corner dofs of the model.
        /// </summary>
        public int NumGlobalCornerDofs { get; private set; }

        /// <summary>
        /// Dof ordering for corner dofs of each subdomain: Each (INode, IDofType) pair of a subdomain is associated with the   
        /// index of that dof into a vector corresponding to corner dofs of that subdomain.
        /// </summary>
        public Dictionary<int, DofTable> SubdomainCornerDofOrderings { get; }

        /// <summary>
        /// Bc unsigned boolean matrices that map global to subdomain corner dofs. This method must be called after 
        /// <see cref="DefineGlobalCornerDofs(Dictionary{int, HashSet{INode}})"/>.
        /// </summary>
        public void CalcCornerMappingMatrices()
        { //TODO: Can I reuse subdomain data? Yes if the global corner dofs have not changed.
            foreach (ISubdomain subdomain in Model.Subdomains)
            {
                int s = subdomain.ID;
                DofTable localCornerDofOrdering = SubdomainCornerDofOrderings[s];
                int numLocalCornerDofs = localCornerDofOrdering.EntryCount;
                var Bc = new UnsignedBooleanMatrix(numLocalCornerDofs, NumGlobalCornerDofs);
                foreach ((INode node, IDofType dofType, int localIdx) in localCornerDofOrdering)
                {
                    int globalIdx = GlobalCornerDofOrdering[node, dofType];
                    Bc.AddEntry(localIdx, globalIdx);
                }
                CornerBooleanMatrices[s] = Bc;
            }
        }

        public void DefineGlobalBoundaryDofs(HashSet<INode> globalCornerNodes)
        {
            IEnumerable<INode> globalRemainderNodes = Model.Nodes.Where(node => !globalCornerNodes.Contains(node));
            GlobalBoundaryDofs =
                DofSeparationUtilities.DefineGlobalBoundaryDofs(globalRemainderNodes, globalDofOrdering.GlobalFreeDofs); //TODO: This could be reused in some cases
        }

        public void DefineGlobalCornerDofs(HashSet<INode> globalCornerNodes)
        {
            // Order global corner dofs and create the global corner to global free map.
            var cornerToGlobalDofs = new List<int>(globalCornerNodes.Count * 3);
            var globalCornerDofOrdering = new DofTable(); //TODO: Should this be cached?
            int cornerDofCounter = 0;
            foreach (INode cornerNode in new SortedSet<INode>(globalCornerNodes)) //TODO: Must they be sorted?
            {
                bool hasFreeDofs = globalDofOrdering.GlobalFreeDofs.TryGetDataOfRow(cornerNode,
                    out IReadOnlyDictionary<IDofType, int> dofsOfNode);
                if (!hasFreeDofs) throw new Exception($"Corner node {cornerNode.ID} has only constrained or embedded dofs.");
                foreach (var dofTypeIdxPair in dofsOfNode)
                {
                    IDofType dofType = dofTypeIdxPair.Key;
                    int globalDofIdx = dofTypeIdxPair.Value;
                    globalCornerDofOrdering[cornerNode, dofType] = cornerDofCounter++;
                    cornerToGlobalDofs.Add(globalDofIdx);
                }
            }
            NumGlobalCornerDofs = cornerDofCounter;
            GlobalCornerToFreeDofMap = cornerToGlobalDofs.ToArray();
            GlobalCornerDofOrdering = globalCornerDofOrdering;
        }
    }
}
