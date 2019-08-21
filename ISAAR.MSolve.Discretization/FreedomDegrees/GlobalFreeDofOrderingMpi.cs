using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Exceptions;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.Discretization.Transfer.Utilities;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MPI;

//TODO: For now each global-subdomain mappings are stored in both in master process and in the corresponding subdomain. However 
//      it may be more efficient to keep them in master and do the work there
//TODO: I implemented the methods as being executed simultaneously for all processes/subdomains. 
//      This is in contrast to the serial implementation that only does one subdomain at a time, meaning different schematics.
//      To use the interface polymorphically, the interface should specify both an all-together version and one-at-a-time.
namespace ISAAR.MSolve.Discretization.FreedomDegrees
{
    public class GlobalFreeDofOrderingMpi : GlobalFreeDofOrderingBase, IGlobalFreeDofOrdering  
    {
        private const int freeDofOrderingTag = 0;

        private readonly Intracommunicator comm;
        private readonly Dictionary<int, INode> globalNodes_master;
        private readonly int masterProcess;
        private readonly ProcessDistribution processDistribution;
        private readonly int rank;
        private readonly ISubdomain subdomain;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="numGlobalFreeDofs">Will be ignored for every process other than <paramref name="masterProcess"/>.</param>
        /// <param name="globalFreeDofs">Will be ignored for every process other than <paramref name="masterProcess"/>.</param>
        /// <param name="subdomainDofOrdering"></param>
        /// <param name="comm"></param>
        /// <param name="masterProcess"></param>
        public GlobalFreeDofOrderingMpi(int numGlobalFreeDofs, DofTable globalFreeDofs,
            Dictionary<int, INode> globalNodes, ISubdomain subdomain, 
            Intracommunicator comm, int masterProcess, ProcessDistribution processDistribution):
            base(numGlobalFreeDofs, globalFreeDofs)
        {
            this.globalNodes_master = globalNodes;
            this.subdomain = subdomain;

            this.comm = comm;
            this.rank = comm.Rank;
            this.masterProcess = masterProcess;
            this.processDistribution = processDistribution;

            //TODO: This should be evaluated lazily by the right process.
            //base.CalcSubdomainGlobalMappings();
        }

        public DofTable GlobalFreeDofs
        {
            get
            {
                if (rank == masterProcess) return globalFreeDofs;
                else throw new MpiException($"Process {rank}: Only defined for master process (rank = {masterProcess})");
            }
        }

        public int NumGlobalFreeDofs //TODO: This can be broadcasted tbh
        {
            get
            {
                if (rank == masterProcess) return numGlobalFreeDofs;
                else throw new MpiException($"Process {rank}: Only defined for master process (rank = {masterProcess})");
            }
        }

        //TODO: this does not work if called only be master or for a subdomain that does not correspond to the process (even for master).
        /// <summary>
        /// 
        /// </summary>
        /// <param name="subdomain"></param>
        /// <param name="subdomainVector">Each process has its own.</param>
        /// <param name="globalVector">Only exists in master process.</param>
        public override void AddVectorSubdomainToGlobal(ISubdomain subdomain, IVectorView subdomainVector, IVector globalVector)
        {
            if (subdomain.ID != this.subdomain.ID) throw new MpiException(
                $"Process {rank}: This process does not have access to subdomain {subdomain.ID})");

            // Gather the subdomain vectors to master
            //TODO: Perhaps client master can work with vectors that have the different portions of the gathered flattened array 
            //      as their backing end. I need dedicated classes for these (e.g. OffsetVector)
            int[] arrayLengths = null;
            if (rank == masterProcess)
            {
                arrayLengths = new int[comm.Size];
                for (int p = 0; p < comm.Size; ++p)
                {
                    ISubdomain sub = processDistribution.ProcesesToSubdomains[p];
                    arrayLengths[p] = subdomainDofOrderings[sub].NumFreeDofs;
                }
            }
            //TODO: The next is stupid, since it copies the vector to an array, while I could access its backing storage in 
            //      most cases. I need a class that handles transfering the concrete vector class. That would live in an 
            //      LinearAlgebra.MPI project
            double[] subdomainArray = null;
            if (rank != masterProcess) subdomainArray = subdomainVector.CopyToArray();
            double[][] subdomainArrays = MpiUtilities.GatherArrays<double>(comm, subdomainArray, arrayLengths, masterProcess);

            // Call base method for each vector
            if (rank == masterProcess)
            {
                for (int p = 0; p < comm.Size; ++p)
                {
                    ISubdomain sub = processDistribution.ProcesesToSubdomains[p];
                    if (p == masterProcess) base.AddVectorSubdomainToGlobal(subdomain, subdomainVector, globalVector);
                    else base.AddVectorSubdomainToGlobal(subdomain, Vector.CreateFromArray(subdomainArrays[p]), globalVector);
                }
            }
            throw new NotImplementedException();
        }

        public override void AddVectorSubdomainToGlobalMeanValue(ISubdomain subdomain, IVectorView subdomainVector,
            IVector globalVector) => throw new NotImplementedException();


        public void CreateSubdomainGlobalMaps(IModel model)
        {
            //TODO: This should be deleted, since this method is explicitly called by the solver or analyzer. No need to decide 
            //      privately when to do actually run this code
            //// If each process has at its subdomain-global dof mapping stored, then return without recalculating them 
            //if (subdomainToGlobalDofMaps != null) return;

            // Gather the dof tables to master
            var transfer = new DofTableTransfer(comm, masterProcess, processDistribution, model.DofSerializer);
            if (rank == masterProcess) transfer.DefineModelData_master(model.EnumerateSubdomains(), globalNodes_master);
            else transfer.DefineSubdomainData_slave(true, subdomainDofOrderings[subdomain].FreeDofs);
            transfer.Transfer(freeDofOrderingTag);
            
            // Assign the subdomain dof orderings
            this.subdomainDofOrderings = new Dictionary<ISubdomain, ISubdomainFreeDofOrdering>();
            if (rank == masterProcess)
            {
                foreach (ISubdomain sub in model.EnumerateSubdomains())
                {
                    if (sub.ID == this.subdomain.ID) subdomainDofOrderings[sub] = this.subdomain.FreeDofOrdering;
                    else
                    {
                        subdomainDofOrderings[sub] = new SubdomainFreeDofOrderingCaching(
                            transfer.NumSubdomainDofs_master[sub.ID], transfer.SubdomainDofOrderings_master[sub.ID]);
                    }
                }
            }

            // Create and store all mapping arrays in master
            if (rank == masterProcess) base.CalcSubdomainGlobalMappings();

            // Scatter them to the corresponding processes
            int[][] allMappings = null;
            if (rank == masterProcess)
            {
                allMappings = new int[comm.Size][];
                for (int p = 0; p < comm.Size; ++p)
                {
                    ISubdomain sub = processDistribution.ProcesesToSubdomains[p];
                    allMappings[p] = subdomainToGlobalDofMaps[sub];
                }
            }
            int numSubdomainDofs = subdomainDofOrderings[subdomain].NumFreeDofs;
            int[] mapping = MpiUtilities.ScatterArrays<int>(comm, allMappings, numSubdomainDofs, masterProcess);

            // Store each one in processes other than master, since it already has all of them.
            if (rank != masterProcess)
            {
                subdomainToGlobalDofMaps = new Dictionary<ISubdomain, int[]>();
                subdomainToGlobalDofMaps[subdomain] = mapping;
            }
        }

        //TODO: this does not work if called only be master or for a subdomain that does not correspond to the process (even for master).
        /// <summary>
        /// 
        /// </summary>
        /// <param name="subdomain"></param>
        /// <param name="globalVector">Only exists in master process.</param>
        /// <param name="subdomainVector">Each process has its own.</param>
        public override void ExtractVectorSubdomainFromGlobal(ISubdomain subdomain, IVectorView globalVector, IVector subdomainVector)
        {
            if (subdomain.ID != this.subdomain.ID) throw new MpiException(
                $"Process {rank}: This process does not have access to subdomain {subdomain.ID})");

            // Broadcast globalVector
            //TODO: The next is stupid, since it copies the vector to an array, while I could access its backing storage in 
            //      most cases. I need a class that handles transfering the concrete vector class. That would live in an 
            //      LinearAlgebra.MPI project
            double[] globalArray = null;
            if (rank == masterProcess) globalArray = globalVector.CopyToArray();
            MpiUtilities.BroadcastArray<double>(comm, ref globalArray, masterProcess);
            if (rank != masterProcess) globalVector = Vector.CreateFromArray(globalArray);

            // Then call base method
            base.ExtractVectorSubdomainFromGlobal(subdomain, globalVector, subdomainVector);
        }

        public ISubdomainFreeDofOrdering GetSubdomainDofOrdering(ISubdomain subdomain)
        {
            if (subdomain.ID == this.subdomain.ID) return subdomainDofOrderings[subdomain]; // No MPI communication needed
            else if (rank == masterProcess) return subdomainDofOrderings[subdomain];
            else throw new MpiException($"Process {rank}: This process does not have access to subdomain {subdomain.ID})");
        }

        public int[] GetSubdomainToGlobalMap(ISubdomain subdomain)
        {
            if (rank == masterProcess) return subdomainToGlobalDofMaps[subdomain];
            else
            {
                if (subdomain.ID == this.subdomain.ID) return subdomainToGlobalDofMaps[subdomain];
                else throw new MpiException($"Process {rank}: This process does not have access to subdomain {subdomain.ID})");
            }
        }
    }
}
