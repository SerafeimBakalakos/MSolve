using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Exceptions;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.Discretization.Transfer.Utilities;
using ISAAR.MSolve.LinearAlgebra.Matrices.Operators;
using ISAAR.MSolve.Solvers.DomainDecomposition.DofSeparation;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.CornerNodes;
using MPI;

//TODO: Perhaps I should also find and expose the indices of boundary remainder and internal remainder dofs into the sequence 
//      of all free dofs of each subdomain
//TODO: Decide which of these data structures will be stored and which will be used ONCE to create all required mapping matrices.
//TODO: Perhaps the corner dof logic should be moved to another class.
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.DofSeparation
{
    public class FetiDPDofSeparatorMpi : IFetiDPDofSeparator
    {
        private const int cornerDofOrderingTag = 0;
        private const int cornerMappingMatrixTag = 1;

        private readonly IModel model;
        private readonly FetiDPGlobalDofSeparator globalDofs;
        private readonly ProcessDistribution procs;
        private readonly ISubdomain processSubdomain;
        private readonly FetiDPSubdomainDofSeparator subdomainDofs;

        // These are defined per subdomain and are needed both in the corresponding process and in master.
        private Dictionary<ISubdomain, UnsignedBooleanMatrix> subdomainCornerBooleanMatrices_master;
        private Dictionary<ISubdomain, DofTable> subdomainCornerDofOrderings_master = new Dictionary<ISubdomain, DofTable>();

        public FetiDPDofSeparatorMpi(ProcessDistribution processDistribution, IModel model)
        {
            this.procs = processDistribution;
            this.model = model;
            this.processSubdomain = model.GetSubdomain(processDistribution.OwnSubdomainID);

            subdomainDofs = new FetiDPSubdomainDofSeparator(model.GetSubdomain(procs.OwnSubdomainID));
            if (procs.IsMasterProcess) globalDofs = new FetiDPGlobalDofSeparator(model);
        }

        public Dictionary<INode, IDofType[]> GlobalBoundaryDofs
        {
            get
            {
                procs.CheckProcessIsMaster();
                return globalDofs.GlobalBoundaryDofs;
            }
        }

        public DofTable GlobalCornerDofOrdering
        {
            get
            {
                procs.CheckProcessIsMaster();
                return globalDofs.GlobalCornerDofOrdering;
            }
        }

        public int[] GlobalCornerToFreeDofMap
        {
            get
            {
                procs.CheckProcessIsMaster();
                return globalDofs.GlobalCornerToFreeDofMap;
            }
        }

        public int NumGlobalCornerDofs
        {
            get
            {
                procs.CheckProcessIsMaster();
                return globalDofs.NumGlobalCornerDofs; //TODO: Shouldn't this be available to all processes?
            }
        }

        public int[] GetBoundaryDofIndices(ISubdomain subdomain)
        {
            procs.CheckProcessMatchesSubdomain(subdomain.ID);
            return subdomainDofs.BoundaryDofIndices;
        }

        public (INode node, IDofType dofType)[] GetBoundaryDofs(ISubdomain subdomain)
        {
            procs.CheckProcessMatchesSubdomain(subdomain.ID);
            return subdomainDofs.BoundaryDofs;
        }

        public UnsignedBooleanMatrix GetCornerBooleanMatrix(ISubdomain subdomain)
        {
            if (procs.IsMasterProcess) return subdomainCornerBooleanMatrices_master[subdomain];
            else
            {
                procs.CheckProcessMatchesSubdomain(subdomain.ID);
                return subdomainDofs.CornerBooleanMatrix;
            }
        }

        public int[] GetCornerDofIndices(ISubdomain subdomain)
        {
            procs.CheckProcessMatchesSubdomain(subdomain.ID);
            return subdomainDofs.CornerDofIndices;
        }

        public DofTable GetCornerDofOrdering(ISubdomain subdomain)
        {
            if (procs.IsMasterProcess) return subdomainCornerDofOrderings_master[subdomain];
            else
            {
                procs.CheckProcessMatchesSubdomain(subdomain.ID);
                return subdomainDofs.CornerDofOrdering;
            }
        }

        public DofTable GetRemainderDofOrdering(ISubdomain subdomain)
        {
            procs.CheckProcessMatchesSubdomain(subdomain.ID);
            return subdomainDofs.RemainderDofOrdering;
        }

        public int[] GetRemainderDofIndices(ISubdomain subdomain)
        {
            procs.CheckProcessMatchesSubdomain(subdomain.ID);
            return subdomainDofs.RemainderDofIndices;
        }

        public int[] GetInternalDofIndices(ISubdomain subdomain)
        {
            procs.CheckProcessMatchesSubdomain(subdomain.ID);
            return subdomainDofs.InternalDofIndices;
        }

        public void SeparateDofs(ICornerNodeSelection cornerNodeSelection, IFetiDPSeparatedDofReordering reordering)
        {
            // Global dofs
            if (procs.IsMasterProcess)
            {
                globalDofs.DefineGlobalBoundaryDofs(cornerNodeSelection.GlobalCornerNodes);
                globalDofs.DefineGlobalCornerDofs(cornerNodeSelection.GlobalCornerNodes);
                globalDofs.ReorderGlobalCornerDofs(reordering.ReorderGlobalCornerDofs(this));
            }

            // Subdomain dofs
            if (processSubdomain.ConnectivityModified)
            {
                int s = processSubdomain.ID;
                Debug.WriteLine($"{this.GetType().Name}: Separating and ordering corner-remainder dofs of subdomain {s}");
                subdomainDofs.SeparateCornerRemainderDofs(cornerNodeSelection.GetCornerNodesOfSubdomain(processSubdomain));

                Debug.WriteLine($"{this.GetType().Name}: Reordering internal dofs of subdomain {s}.");
                subdomainDofs.ReorderRemainderDofs(reordering.ReorderSubdomainRemainderDofs(processSubdomain, this));

                Debug.WriteLine($"{this.GetType().Name}: Separating and ordering boundary-internal dofs of subdomain {s}");
                subdomainDofs.SeparateBoundaryInternalDofs(cornerNodeSelection.GetCornerNodesOfSubdomain(processSubdomain));
            }

            // Subdomain - global mappings
            CalcCornerMappingMatrices();
        }

        /// <summary>
        /// Bc unsigned boolean matrices that map global to subdomain corner dofs. This method must be called after 
        /// <see cref="DefineGlobalCornerDofs(Dictionary{int, HashSet{INode}})"/> and after the reordering of the global corner 
        /// dofs has been calculated.
        /// </summary>
        private void CalcCornerMappingMatrices()
        {
            GatherCornerDofOrderingsFromSubdomains();
            if (procs.IsMasterProcess)
            {
                subdomainCornerBooleanMatrices_master = globalDofs.CalcCornerMappingMatrices(subdomainCornerDofOrderings_master);
            }
            ScatterCornerBooleanMatricesToSubdomains();
        }

        //TODO: The solver defines which subdomains are modified, but how? 
        //      Perhaps the object that provides corner nodes should also inform if they have changed.
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
            if (procs.IsMasterProcess)
            {
                // Only process the subdomains that have changed and need to update their dof orderings.
                IEnumerable<ISubdomain> modifiedSubdomains = model.EnumerateSubdomains().Where(sub => sub.ConnectivityModified); //TODO: Is this what I should check?
                transfer.DefineModelData_master(modifiedSubdomains);
            }
            else
            {
                transfer.DefineSubdomainData_slave(subdomainDofs.Subdomain.ConnectivityModified,
                    subdomainDofs.CornerDofOrdering);
            }
            transfer.Transfer(cornerDofOrderingTag);

            if (procs.IsMasterProcess)
            {
                // Assign the received orderings
                foreach (ISubdomain sub in transfer.SubdomainDofOrderings_master.Keys)
                {
                    subdomainCornerDofOrderings_master[sub] = transfer.SubdomainDofOrderings_master[sub];
                }

                // For the subdomain of the master process, just copy the reference. Even if it isn't updated, it is costless.
                subdomainCornerDofOrderings_master[processSubdomain] = subdomainDofs.CornerDofOrdering;
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
                    matricesBc[p] = subdomainCornerBooleanMatrices_master[model.GetSubdomain(p)];
                }
            }

            // Scatter the matrices. // TODO: This will use the automatic serialization of MPI.NET. Should I write something custom for this matrix type?
            UnsignedBooleanMatrix Bc = procs.Communicator.Scatter(matricesBc, procs.MasterProcess);
            subdomainDofs.SetCornerBooleanMatrix(Bc, this);
        }


    }
}
