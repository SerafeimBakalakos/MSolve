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

        private readonly ProcessDistribution procs;
        private readonly IModel model;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="numGlobalFreeDofs">Will be ignored for every process other than <paramref name="processes.MasterProcess"/>.</param>
        /// <param name="globalFreeDofs">Will be ignored for every process other than <paramref name="processes.MasterProcess"/>.</param>
        public GlobalFreeDofOrderingMpi(ProcessDistribution processDistribution, int numGlobalFreeDofs, DofTable globalFreeDofs, 
            IModel model):
            base(numGlobalFreeDofs, globalFreeDofs)
        {
            this.model = model;
            this.procs = processDistribution;
        }

        public DofTable GlobalFreeDofs
        {
            get
            {
                MpiException.CheckProcessIsMaster(procs);
                return globalFreeDofs;
            }
        }

        public int NumGlobalFreeDofs //TODO: This can be broadcasted tbh
        {
            get
            {
                MpiException.CheckProcessIsMaster(procs);
                return numGlobalFreeDofs;
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
            MpiException.CheckProcessMatchesSubdomain(procs, subdomain.ID);

            // Gather the subdomain vectors to master
            //TODO: Perhaps client master can work with vectors that have the different portions of the gathered flattened array 
            //      as their backing end. I need dedicated classes for these (e.g. OffsetVector)
            int[] arrayLengths = null;
            if (procs.IsMasterProcess)
            {
                arrayLengths = new int[procs.Communicator.Size];
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    int sub = procs.GetSubdomainIdOfProcess(p);
                    arrayLengths[p] = subdomainDofOrderings[sub].NumFreeDofs;
                }
            }
            //TODO: The next is stupid, since it copies the vector to an array, while I could access its backing storage in 
            //      most cases. I need a class that handles transfering the concrete vector class. That would live in an 
            //      LinearAlgebra.MPI project
            double[] subdomainArray = null;
            if (!procs.IsMasterProcess) subdomainArray = subdomainVector.CopyToArray();
            double[][] subdomainArrays = MpiUtilities.GatherArrays<double>(procs.Communicator, subdomainArray, arrayLengths, 
                procs.MasterProcess);

            // Call base method for each vector
            if (procs.IsMasterProcess)
            {
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    if (p == procs.MasterProcess) base.AddVectorSubdomainToGlobal(subdomain, subdomainVector, globalVector);
                    else
                    {
                        ISubdomain sub = model.GetSubdomain(procs.GetSubdomainIdOfProcess(p));
                        base.AddVectorSubdomainToGlobal(sub, Vector.CreateFromArray(subdomainArrays[p]), globalVector);
                    }
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
            var transfer = new DofTableTransfer(model, procs);
            if (procs.IsMasterProcess) transfer.DefineModelData_master(model.EnumerateSubdomains());
            else transfer.DefineSubdomainData_slave(true, subdomainDofOrderings[procs.OwnSubdomainID].FreeDofs);
            transfer.Transfer(freeDofOrderingTag);
            
            // Assign the subdomain dof orderings
            this.subdomainDofOrderings = new Dictionary<int, ISubdomainFreeDofOrdering>();
            if (procs.IsMasterProcess)
            {
                foreach (ISubdomain sub in model.EnumerateSubdomains())
                {
                    if (sub.ID == procs.OwnSubdomainID)
                    {
                        subdomainDofOrderings[sub.ID] = model.GetSubdomain(procs.OwnSubdomainID).FreeDofOrdering;
                    }
                    else
                    {
                        subdomainDofOrderings[sub.ID] = new SubdomainFreeDofOrderingCaching(
                            transfer.NumSubdomainDofs_master[sub], transfer.SubdomainDofOrderings_master[sub]);
                    }
                }
            }

            // Create and store all mapping arrays in master
            if (procs.IsMasterProcess) base.CalcSubdomainGlobalMappings();

            // Scatter them to the corresponding processes
            int[][] allMappings = null;
            if (procs.IsMasterProcess)
            {
                allMappings = new int[procs.Communicator.Size][];
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    int sub = procs.GetSubdomainIdOfProcess(p);
                    allMappings[p] = subdomainToGlobalDofMaps[sub];
                }
            }
            int numSubdomainDofs = subdomainDofOrderings[procs.OwnSubdomainID].NumFreeDofs;
            int[] mapping = MpiUtilities.ScatterArrays<int>(procs.Communicator, allMappings, numSubdomainDofs, procs.MasterProcess);

            // Store each one in processes other than master, since it already has all of them.
            if (!procs.IsMasterProcess)
            {
                subdomainToGlobalDofMaps = new Dictionary<int, int[]>();
                subdomainToGlobalDofMaps[procs.OwnSubdomainID] = mapping;
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
            MpiException.CheckProcessMatchesSubdomain(procs, subdomain.ID);

            // Broadcast globalVector
            //TODO: The next is stupid, since it copies the vector to an array, while I could access its backing storage in 
            //      most cases. I need a class that handles transfering the concrete vector class. That would live in an 
            //      LinearAlgebra.MPI project
            double[] globalArray = null;
            if (procs.IsMasterProcess) globalArray = globalVector.CopyToArray();
            MpiUtilities.BroadcastArray<double>(procs.Communicator, ref globalArray, procs.MasterProcess);
            if (!procs.IsMasterProcess) globalVector = Vector.CreateFromArray(globalArray);

            // Then call base method
            base.ExtractVectorSubdomainFromGlobal(subdomain, globalVector, subdomainVector);
        }

        public ISubdomainFreeDofOrdering GetSubdomainDofOrdering(ISubdomain subdomain)
        {
            MpiException.CheckProcessMatchesSubdomainUnlessMaster(procs, subdomain.ID);
            return subdomainDofOrderings[subdomain.ID];
        }

        public int[] MapSubdomainToGlobalDofs(ISubdomain subdomain)
        {
            MpiException.CheckProcessMatchesSubdomainUnlessMaster(procs, subdomain.ID);
            return subdomainToGlobalDofMaps[subdomain.ID];
        }
    }
}
