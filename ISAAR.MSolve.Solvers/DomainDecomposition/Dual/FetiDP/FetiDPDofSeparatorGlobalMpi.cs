//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using ISAAR.MSolve.Discretization.FreedomDegrees;
//using ISAAR.MSolve.Discretization.Interfaces;
//using ISAAR.MSolve.Solvers.DomainDecomposition.DofSeparation;

//namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP
//{
//    public class FetiDPDofSeparatorGlobalMpi
//    {
//        /// <summary>
//        /// Dofs where Lagrange multipliers will be applied. These do not include corner dofs.
//        /// </summary>
//        public Dictionary<INode, IDofType[]> GlobalBoundaryDofs { get; private set; }

//        /// <summary>
//        /// Dof ordering for corner dofs of the model: Each (INode, IDofType) pair is associated with the index of that dof into 
//        /// a vector corresponding to all corner dofs of the model.
//        /// </summary>
//        public DofTable GlobalCornerDofOrdering { get; private set; } // Only for master

//        /// <summary>
//        /// If Xf is a vector with all free dofs of the model and Xc is a vector with all corner dofs of the model, then
//        /// Xf[GlobalCornerToFreeDofMap[i]] = Xc[i].
//        /// </summary>
//        public int[] GlobalCornerToFreeDofMap { get; set; } // Only for master

//        /// <summary>
//        /// The number of corner dofs of the model.
//        /// </summary>
//        public int NumGlobalCornerDofs { get; private set; }

//        public void DefineGlobalBoundaryDofs(IStructuralModel model, HashSet<INode> globalCornerNodes)
//        {
//            IEnumerable<INode> globalRemainderNodes = model.Nodes.Where(node => !globalCornerNodes.Contains(node));
//            GlobalBoundaryDofs = DofSeparationUtilities.DefineGlobalBoundaryDofs(globalRemainderNodes, model.GlobalDofOrdering); //TODO: This could be reused in some cases
//        }

//        public void DefineGlobalCornerDofs(IStructuralModel model, HashSet<INode> globalCornerNodes)
//        {
//            // Order global corner dofs and create the global corner to global free map.
//            var cornerToGlobalDofs = new List<int>(globalCornerNodes.Count * 3);
//            var globalCornerDofOrdering = new DofTable(); //TODO: Should this be cached?
//            int cornerDofCounter = 0;
//            foreach (INode cornerNode in new SortedSet<INode>(globalCornerNodes)) //TODO: Must they be sorted?
//            {
//                bool hasFreeDofs = model.GlobalDofOrdering.GlobalFreeDofs.TryGetDataOfRow(cornerNode,
//                    out IReadOnlyDictionary<IDofType, int> dofsOfNode);
//                if (!hasFreeDofs) throw new Exception($"Corner node {cornerNode.ID} has only constrained or embedded dofs.");
//                foreach (var dofTypeIdxPair in dofsOfNode)
//                {
//                    IDofType dofType = dofTypeIdxPair.Key;
//                    int globalDofIdx = dofTypeIdxPair.Value;
//                    globalCornerDofOrdering[cornerNode, dofType] = cornerDofCounter++;
//                    cornerToGlobalDofs.Add(globalDofIdx);
//                }
//            }
//            NumGlobalCornerDofs = cornerDofCounter;
//            GlobalCornerToFreeDofMap = cornerToGlobalDofs.ToArray();
//            GlobalCornerDofOrdering = globalCornerDofOrdering;
//        }
//    }
//}
