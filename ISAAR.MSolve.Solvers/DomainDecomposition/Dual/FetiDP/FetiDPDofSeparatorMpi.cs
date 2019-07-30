//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using ISAAR.MSolve.Discretization.FreedomDegrees;
//using ISAAR.MSolve.Discretization.Interfaces;
//using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
//using ISAAR.MSolve.Solvers.DomainDecomposition.DofSeparation;
//using MPI;

////TODO: Remove code duplication between this and Feti1DofSeparator
////TODO: Perhaps I should also find and expose the indices of boundary remainder and internal remainder dofs into the sequence 
////      of all free dofs of each subdomain
////TODO: Decide which of these data structures will be stored and which will be used ONCE to create all required mapping matrices.
////TODO: Perhaps the corner dof logic should be moved to another class.
//namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP
//{
//    public class FetiDPDofSeparatorMpi //: IDofSeparatorMpi
//    {
//        private readonly Intracommunicator comm;
//        private readonly int masterProcess;
//        private readonly int rank;

//        private FetiDPDofSeparatorMpi(Intracommunicator comm, int masterProcess)
//        {
//            this.comm = comm;
//            this.masterProcess = masterProcess;
//            this.rank = comm.Rank;
//        }

//        public FetiDPDofSeparatorSubdomainMpi SubdomainDofs { get; }

//        /// <summary>
//        /// (Node, IDofType) pairs for each boundary remainder dof of each subdomain. Their order is the same one as 
//        /// <see cref="BoundaryDofIndices"/>.
//        /// </summary>
//        public Dictionary<int, (INode node, IDofType dofType)[]> BoundaryDofs { get; private set; } //These are stored both in the process of each subdomain and in the master process.

//        /// <summary>
//        /// Also called Bc in papers by Farhat or Lc in NTUA theses. 
//        /// </summary>
//        public Dictionary<int, UnsignedBooleanMatrix> CornerBooleanMatrices { get; private set; } // Master creates, stores and scatters them. They are needed per process and globally.

//        public FetiDPDofSeparatorMpi()
//        {
//            BoundaryDofs = new Dictionary<int, (INode node, IDofType dofType)[]>();
//            CornerBooleanMatrices = new Dictionary<int, UnsignedBooleanMatrix>();
//        }

//        /// <summary>
//        /// Bc unsigned boolean matrices that map global to subdomain corner dofs. This method must be called after 
//        /// <see cref="DefineGlobalCornerDofs(IStructuralModel, Dictionary{int, HashSet{INode}})"/>.
//        /// </summary>
//        public void CalcCornerMappingMatrices(IStructuralModel model)
//        { //TODO: Can I reuse subdomain data? Yes if the global corner dofs have not changed.
//            foreach (ISubdomain subdomain in model.Subdomains)
//            {
//                int s = subdomain.ID;
//                DofTable localCornerDofOrdering = SubdomainCornerDofOrderings[s];
//                int numLocalCornerDofs = localCornerDofOrdering.EntryCount;
//                var Bc = new UnsignedBooleanMatrix(numLocalCornerDofs, NumGlobalCornerDofs);
//                foreach ((INode node, IDofType dofType, int localIdx) in localCornerDofOrdering)
//                {
//                    int globalIdx = GlobalCornerDofOrdering[node, dofType];
//                    Bc.AddEntry(localIdx, globalIdx);
//                }
//                CornerBooleanMatrices[s] = Bc;
//            }
//        }
//    }
//}
