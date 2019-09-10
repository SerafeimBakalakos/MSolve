using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Exceptions;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.Discretization.Transfer.Utilities;
using ISAAR.MSolve.LinearAlgebra.Output;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MPI;

//TODO: For now each global-subdomain mappings are stored in both in master process and in the corresponding subdomain. However 
//      it may be more efficient to keep them in master and do the work there
//TODO: I implemented the methods as being executed simultaneously for all processes/subdomains. 
//      This is in contrast to the serial implementation that only does one subdomain at a time, meaning different schematics.
//      To use the interface polymorphically, the interface should specify both an all-together version and one-at-a-time.
namespace ISAAR.MSolve.Discretization.FreedomDegrees
{
    public class GlobalFreeDofOrderingMpi : IGlobalFreeDofOrdering
    {
        private const int freeDofOrderingTag = 0;

        private readonly DofTable globalFreeDofs_master;
        private readonly IModel model;
        private readonly int numGlobalFreeDofs_master;
        private readonly ProcessDistribution procs;

        private bool hasGatheredSubdomainOrderings = false;
        private bool hasCreatedSubdomainGlobalMaps = false;
        private bool hasScatteredSubdomainGlobalMaps = false;

        /// <summary>
        /// Master contains all orderings. All other process only contain the corresponding subdomain data.
        /// </summary>
        private Dictionary<int, ISubdomainFreeDofOrdering> subdomainDofOrderings;

        /// <summary>
        /// Master contains all maps. All other process only contain the corresponding subdomain data.
        /// </summary>
        private Dictionary<int, int[]> subdomainToGlobalDofMaps;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="numGlobalFreeDofs">Will be ignored for every process other than <paramref name="processes.MasterProcess"/>.</param>
        /// <param name="globalFreeDofs">Will be ignored for every process other than <paramref name="processes.MasterProcess"/>.</param>
        public GlobalFreeDofOrderingMpi(ProcessDistribution processDistribution, IModel model, int numGlobalFreeDofs,
            DofTable globalFreeDofs)
        {
            this.procs = processDistribution;
            this.model = model;
            this.numGlobalFreeDofs_master = numGlobalFreeDofs;
            this.globalFreeDofs_master = globalFreeDofs;

            this.subdomainDofOrderings = new Dictionary<int, ISubdomainFreeDofOrdering>();
            ISubdomain subdomain = model.GetSubdomain(procs.OwnSubdomainID);
            subdomainDofOrderings[subdomain.ID] = subdomain.FreeDofOrdering;
        }

        public DofTable GlobalFreeDofs
        {
            get
            {
                procs.CheckProcessIsMaster();
                return globalFreeDofs_master;
            }
        }

        public int NumGlobalFreeDofs //TODO: This can be broadcasted tbh
        {
            get
            {
                procs.CheckProcessIsMaster();
                return numGlobalFreeDofs_master;
            }
        }

        //TODO: this does not work if called only be master or for a subdomain that does not correspond to the process (even for master).
        /// <summary>
        /// 
        /// </summary>
        /// <param name="subdomain"></param>
        /// <param name="subdomainVector">Each process has its own.</param>
        /// <param name="globalVector">Only exists in master process.</param>
        public void AddVectorSubdomainToGlobal(ISubdomain subdomain, IVectorView subdomainVector, IVector globalVector)
        {
            procs.CheckProcessMatchesSubdomain(subdomain.ID);
            ScatterSubdomainGlobalMaps();

            // Gather the subdomain vectors to master
            //TODO: Perhaps client master can work with vectors that have the different portions of the gathered flattened array 
            //      as their backing end. I need dedicated classes for these (e.g. OffsetVector)
            int[] arrayLengths = null;
            if (procs.IsMasterProcess)
            {
                arrayLengths = new int[procs.Communicator.Size];
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    if (p == procs.MasterProcess) arrayLengths[p] = 0;
                    else
                    {
                        int sub = procs.GetSubdomainIdOfProcess(p);
                        arrayLengths[p] = subdomainDofOrderings[sub].NumFreeDofs;
                    }
                }
            }

            //TODO: The next is stupid, since it copies the vector to an array, while I could access its backing storage in 
            //      most cases. I need a class that handles transfering the concrete vector class. That would live in an 
            //      LinearAlgebra.MPI project
            double[] subdomainArray = null;
            if (procs.IsMasterProcess) subdomainArray = new double[0];
            else subdomainArray = subdomainVector.CopyToArray();
            double[][] subdomainArrays = MpiUtilities.GatherArrays<double>(procs.Communicator, subdomainArray, arrayLengths,
                procs.MasterProcess);

            // Call base method for each vector
            if (procs.IsMasterProcess)
            {
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    IVectorView localVector = null;
                    if (p == procs.MasterProcess) localVector = subdomainVector;
                    else localVector = Vector.CreateFromArray(subdomainArrays[p]);

                    int[] subdomainToGlobalDofs = subdomainToGlobalDofMaps[procs.GetSubdomainIdOfProcess(p)];
                    globalVector.AddIntoThisNonContiguouslyFrom(subdomainToGlobalDofs, localVector);
                }
            }
        }

        public void AddVectorSubdomainToGlobalMeanValue(ISubdomain subdomain, IVectorView subdomainVector,
            IVector globalVector) => throw new NotImplementedException();


        //TODO: this does not work if called only be master or for a subdomain that does not correspond to the process (even for master).
        /// <summary>
        /// 
        /// </summary>
        /// <param name="subdomain"></param>
        /// <param name="globalVector">Only exists in master process.</param>
        /// <param name="subdomainVector">Each process has its own.</param>
        public void ExtractVectorSubdomainFromGlobal(ISubdomain subdomain, IVectorView globalVector, IVector subdomainVector)
        {
            procs.CheckProcessMatchesSubdomain(subdomain.ID);

            ScatterSubdomainGlobalMaps();

            // Broadcast globalVector
            //TODO: The next is stupid, since it copies the vector to an array, while I could access its backing storage in 
            //      most cases. I need a class that handles transfering the concrete vector class. That would live in an 
            //      LinearAlgebra.MPI project
            double[] globalArray = null;
            if (procs.IsMasterProcess) globalArray = globalVector.CopyToArray();
            MpiUtilities.BroadcastArray<double>(procs.Communicator, ref globalArray, procs.MasterProcess);
            if (!procs.IsMasterProcess) globalVector = Vector.CreateFromArray(globalArray);

            // Then do the actual work
            int[] subdomainToGlobalDofs = subdomainToGlobalDofMaps[subdomain.ID];
            subdomainVector.CopyNonContiguouslyFrom(globalVector, subdomainToGlobalDofs);
        }

        public ISubdomainFreeDofOrdering GetSubdomainDofOrdering(ISubdomain subdomain)
        {
            procs.CheckProcessMatchesSubdomain(subdomain.ID);
            return subdomainDofOrderings[subdomain.ID];
        }

        public int[] MapSubdomainToGlobalDofs(ISubdomain subdomain)
        {
            procs.CheckProcessMatchesSubdomainUnlessMaster(subdomain.ID);
            ScatterSubdomainGlobalMaps();
            return subdomainToGlobalDofMaps[subdomain.ID];
        }

        private void GatherSubdomainDofOrderings()
        {
            if (hasGatheredSubdomainOrderings) return;

            // Gather the dof tables to master
            var transfer = new DofTableTransfer(model, procs);
            if (procs.IsMasterProcess) transfer.DefineModelData_master(model.EnumerateSubdomains());
            else transfer.DefineSubdomainData_slave(true, subdomainDofOrderings[procs.OwnSubdomainID].FreeDofs);
            transfer.Transfer(freeDofOrderingTag);

            // Assign the subdomain dof orderings
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

            hasGatheredSubdomainOrderings = true;
        }

        private void CreateSubdomainGlobalMaps() //TODO: Split this into more methods that are lazily called.
        {
            if (hasCreatedSubdomainGlobalMaps) return;

            GatherSubdomainDofOrderings();

            // Create and store all mapping arrays in master
            if (procs.IsMasterProcess)
            {
                subdomainToGlobalDofMaps = 
                    GlobalFreeDofOrderingUtilities.CalcSubdomainGlobalMappings(globalFreeDofs_master, subdomainDofOrderings);
            }
            hasCreatedSubdomainGlobalMaps = true;
        }

        private void ScatterSubdomainGlobalMaps()
        {
            if (hasScatteredSubdomainGlobalMaps) return;

            CreateSubdomainGlobalMaps();

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

            hasScatteredSubdomainGlobalMaps = true;
        }
    }
}
