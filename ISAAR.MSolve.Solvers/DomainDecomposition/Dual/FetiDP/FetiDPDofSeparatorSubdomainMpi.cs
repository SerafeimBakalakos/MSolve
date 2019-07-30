//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using ISAAR.MSolve.Discretization.FreedomDegrees;
//using ISAAR.MSolve.Discretization.Interfaces;
//using ISAAR.MSolve.Solvers.DomainDecomposition.DofSeparation;

//namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP
//{
//    //TODO: This should be decoupled from MPI logic, so that it can be used in serial code too.
//    public class FetiDPDofSeparatorSubdomainMpi
//    {
//        /// <summary>
//        /// Indices of boundary remainder dofs into the sequence of all remainder dofs of a subdomain.
//        /// </summary>
//        public int[] BoundaryDofIndices { get; private set; } //These can live inside each process. They are only needed globally if GatherGlobalDisplacements() is called (which normally won't be). In this case, they must be gathered.

//        /// <summary>
//        /// (Node, IDofType) pairs for each boundary remainder dof of a subdomain. Their order is the same one as 
//        /// <see cref="BoundaryDofIndices"/>.
//        /// </summary>
//        public (INode node, IDofType dofType)[] BoundaryDofs { get; private set; } //These are stored both in the process of each subdomain and in the master process.

//        /// <summary>
//        /// Indices of (boundary) corner dofs into the sequence of all free dofs of a subdomain.
//        /// </summary>
//        public int[] CornerDofIndices { get; private set; }

//        /// <summary>
//        /// Indices of internal remainder dofs into the sequence of all remainder dofs of a subdomain.
//        /// </summary>
//        public int[] InternalDofIndices { get; private set; } //These can live inside each process. They are only needed globally if GatherGlobalDisplacements() is called (which normally won't be). In this case, they must be gathered.

//        /// <summary>
//        /// Dof ordering for remainder (boundary and internal) dofs of a subdomain: Each (INode, IDofType) pair of the 
//        /// subdomain is associated with the index of that dof into a vector corresponding to remainder dofs of that subdomain.
//        /// </summary>
//        public DofTable RemainderDofOrdering { get; private set; }

//        /// <summary>
//        /// Indices of remainder (boundary and internal) dofs into the sequence of all free dofs of a subdomain.
//        /// </summary>
//        public int[] RemainderDofIndices { get; private set; }

//        /// <summary>
//        /// Dof ordering for corner dofs of a subdomain: Each (INode, IDofType) pair of the subdomain is associated with the   
//        /// index of that dof into a vector corresponding to corner dofs of that subdomain.
//        /// </summary>
//        public DofTable CornerDofOrdering { get; private set; }

//        /// <summary>
//        /// This must be called after <see cref="SeparateCornerRemainderDofs(ISubdomain, HashSet{INode}, IEnumerable{INode})"/> 
//        /// and after a reordering for the remainder dofs is computed.
//        /// </summary>
//        public void SeparateBoundaryInternalDofs(ISubdomain subdomain, HashSet<INode> cornerNodes)
//        {
//            IEnumerable<INode> remainderAndConstrainedNodes = subdomain.Nodes.Where(node => !cornerNodes.Contains(node));

//            (int[] internalDofIndices, int[] boundaryDofIndices, (INode node, IDofType dofType)[] boundaryDofConnectivities)
//                = DofSeparationUtilities.SeparateBoundaryInternalDofs(remainderAndConstrainedNodes, RemainderDofOrdering);
//            InternalDofIndices = internalDofIndices;
//            BoundaryDofIndices = boundaryDofIndices;
//            BoundaryDofs = boundaryDofConnectivities;
//        }

//        public void SeparateCornerRemainderDofs(ISubdomain subdomain, HashSet<INode> cornerNodes)
//        {
//            IEnumerable<INode> remainderAndConstrainedNodes = subdomain.Nodes.Where(node => !cornerNodes.Contains(node));

//            var cornerDofs = new List<int>();
//            var remainderDofs = new List<int>();
//            foreach (INode node in cornerNodes)
//            {
//                IEnumerable<int> dofsOfNode = subdomain.FreeDofOrdering.FreeDofs.GetValuesOfRow(node);
//                cornerDofs.AddRange(dofsOfNode);
//            }
//            foreach (INode node in remainderAndConstrainedNodes)
//            {
//                IEnumerable<int> dofsOfNode = subdomain.FreeDofOrdering.FreeDofs.GetValuesOfRow(node);
//                remainderDofs.AddRange(dofsOfNode);
//            }
//            CornerDofIndices = cornerDofs.ToArray();
//            RemainderDofIndices = remainderDofs.ToArray();

//            // This dof ordering will be optimized, such that the factorization of Krr is efficient.
//            RemainderDofOrdering = subdomain.FreeDofOrdering.FreeDofs.GetSubtableForNodes(remainderAndConstrainedNodes);
//            CornerDofOrdering = subdomain.FreeDofOrdering.FreeDofs.GetSubtableForNodes(cornerNodes);
//        }
//    }
//}
