using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.Discretization.Transfer.Utilities;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using MPI;

//TODO: Perhaps I should also find and expose the indices of boundary remainder and internal remainder dofs into the sequence 
//      of all free dofs of each subdomain
//TODO: Decide which of these data structures will be stored and which will be used ONCE to create all required mapping matrices.
//TODO: Perhaps the corner dof logic should be moved to another class.
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP
{
    public class FetiDPDofSeparatorMpi //: IDofSeparatorMpi
    {
        private const int cornerDofOrderingTag = 0;
        private const int cornerMappingMatrixTag = 1;

        private readonly IModel model;
        private readonly ProcessDistribution procs;

        public FetiDPDofSeparatorMpi(ProcessDistribution processDistribution, IModel model)
        {
            this.model = model;
            this.procs = processDistribution;

            SubdomainDofs = new FetiDPDofSeparatorSubdomainMpi(model.GetSubdomain(procs.OwnSubdomainID));
            if (procs.IsMasterProcess) GlobalDofs = new FetiDPDofSeparatorGlobalMpi(model);
        }

        //TODO: Ideally the next two are not dependent on the MPI implementation. Only this class is and it handles commnication.
        public FetiDPDofSeparatorGlobalMpi GlobalDofs { get; }
        public FetiDPDofSeparatorSubdomainMpi SubdomainDofs { get; }

        /// <summary>
        /// Bc unsigned boolean matrices that map global to subdomain corner dofs. This method must be called after 
        /// <see cref="DefineGlobalCornerDofs(Dictionary{int, HashSet{INode}})"/> and after the reordering of the global corner 
        /// dofs has been calculated.
        /// </summary>
        public void CalcCornerMappingMatrices()
        { 
            // Create the corner mapping matrices
            if (procs.IsMasterProcess) GlobalDofs.CalcCornerMappingMatrices();
            ScatterCornerBooleanMatricesToSubdomains();

            // Send the corner mapping matrix of each subdomain to the corresponding process
            //TODO: This must be done after finding a reordering for the corner dofs. See solver.
        }

        public void DefineGlobalBoundaryDofs(HashSet<INode> globalCornerNodes)
        {
            if (procs.IsMasterProcess) GlobalDofs.DefineGlobalBoundaryDofs(globalCornerNodes);
        }

        public void DefineGlobalCornerDofs(HashSet<INode> globalCornerNodes)
        {
            if (procs.IsMasterProcess) GlobalDofs.DefineGlobalCornerDofs(globalCornerNodes);
        }

        /// <summary>
        /// This must be called after <see cref="SeparateCornerRemainderDofs(ISubdomain, HashSet{INode}, IEnumerable{INode})"/> 
        /// and after a reordering for the remainder dofs is computed.
        /// </summary>
        public void SeparateBoundaryInternalDofs(HashSet<INode> subdomainCornerNodes)
            => SubdomainDofs.SeparateBoundaryInternalDofs(subdomainCornerNodes);

        public void SeparateCornerRemainderDofs(HashSet<INode> subdomainCornerNodes)
        {
            SubdomainDofs.SeparateCornerRemainderDofs(subdomainCornerNodes);
            GatherCornerDofOrderingsFromSubdomains();
        }

        //TODO: The solver defines which subdomains are modified, but how? 
        //      Perhaps the object that provides corner nodes should also inform if they have changed.
        //TODO: Perhaps this should not be called by the solver independently. Instead, the solver defines the modified 
        //      subdomains and then calls one method from the dof separator that then calls all these individual methods. This 
        //      method in particular, must be called after SeparateCornerRemainderDofs() and before reordering the corner dofs
        //      which weirdly is delegated to another strategy object (IFetiDPCoarseProblemSolver).
        //TODO: There are alternative ways to get these in master process. E.g. The master process could redo the work required 
        //      to create it. Not sure if it will be slower. Should I make this a strategy? If you think about it, this is 
        //      probably the only thing that is fundamentally different between serial and MPI implementations. 
        //TODO: Add optimization in case all subdomains are modified (e.g. at the start of an analysis). In this case, use
        //      MPI gather, since it is faster than send/receive.
        private void GatherCornerDofOrderingsFromSubdomains()
        {
            var tableSerializer = new DofTableSerializer(model.DofSerializer);

            // Gather the corner dof ordering of each subdomain from the corresponding process to master
            var transfer = new DofTableTransfer(model, procs);
            IEnumerable<ISubdomain> modifiedSubdomains = null;
            if (procs.IsMasterProcess)
            {
                // Only process the subdomains that have changed and need to update their dof orderings.
                modifiedSubdomains = model.EnumerateSubdomains().Where(sub => sub.ConnectivityModified); //TODO: Is this what I should check?
                transfer.DefineModelData_master(modifiedSubdomains);
            }
            else
            {
                transfer.DefineSubdomainData_slave(SubdomainDofs.Subdomain.ConnectivityModified,
                    SubdomainDofs.CornerDofOrdering);
            }
            transfer.Transfer(cornerDofOrderingTag);

            if (procs.IsMasterProcess)
            {
                // Assign the received orderings
                foreach (int s in transfer.SubdomainDofOrderings_master.Keys)
                {
                    GlobalDofs.SubdomainCornerDofOrderings[s] = transfer.SubdomainDofOrderings_master[s];
                }

                // For the subdomain of the master process, just copy the reference. Even if it isn't updated, it is costless.
                GlobalDofs.SubdomainCornerDofOrderings[this.SubdomainDofs.Subdomain.ID] = SubdomainDofs.CornerDofOrdering;
            }
        }

        /// <summary>
        /// This should not be called, unless at least one corner dof of the whole model is modified.
        /// </summary>
        /// <param name="subdomain"></param>
        private void ScatterCornerBooleanMatricesToSubdomains()
        {
            UnsignedBooleanMatrix[] matricesBc = null;
            
            // Place the data to scatter in an array, since MPI does not work with dictionaries
            if (procs.IsMasterProcess)
            {
                matricesBc = new UnsignedBooleanMatrix[procs.Communicator.Size];
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    int subdomainID = procs.GetSubdomainIdOfProcess(p);
                    matricesBc[p] = GlobalDofs.CornerBooleanMatrices[subdomainID];
                }
            }

            // Scatter the matrices. // TODO: This will use the automatic serialization of MPI.NET. Should I write something custom for this matrix type?
            SubdomainDofs.CornerBooleanMatrix = procs.Communicator.Scatter(matricesBc, procs.MasterProcess);
        }
    }
}
