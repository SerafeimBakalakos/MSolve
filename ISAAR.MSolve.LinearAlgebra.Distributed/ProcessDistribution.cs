using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Distributed.Exceptions;
using MPI;

//TODO: What about the case where more than one subdomains are delegated to the same process?
//TODO: Remove the subdomains and keep only clusters
//TODO: If there are point2point communications between two processes, should this be transfered to all of them?
namespace ISAAR.MSolve.LinearAlgebra.Distributed
{
    /// <summary>
    /// This should be present in all processes.
    /// </summary>
    public class ProcessDistribution
    {
        //private readonly int[] processesToSubdomains;
        private readonly int[][] processesToSubdomains;
        //private readonly Dictionary<int, int> subdomainsToProcesses;

        public ProcessDistribution(Intracommunicator comm, int masterProcess, int[][] processRanksToSubdomainIDs)
        {
            this.Communicator = comm;
            this.IsMasterProcess = comm.Rank == masterProcess;
            this.MasterProcess = masterProcess;
            this.OwnRank = comm.Rank;

            this.OwnSubdomainID = processRanksToSubdomainIDs[OwnRank][0]; //TODO: Remove this

            this.processesToSubdomains = processRanksToSubdomainIDs;
            //this.subdomainsToProcesses = new Dictionary<int, int>();
            NumSubdomainsTotal = 0;
            for (int p = 0; p < comm.Size; ++p)
            {
                NumSubdomainsTotal += processRanksToSubdomainIDs[p].Length;
                //foreach (int s in processRanksToSubdomainIDs[p]) this.subdomainsToProcesses[s] = p;
            }
        }

        public Intracommunicator Communicator { get; }
        public bool IsMasterProcess { get; }
        public int MasterProcess { get; }
        public int NumSubdomainsTotal { get; }
        public int OwnRank { get; }
        public int OwnSubdomainID { get; }

        [Conditional("DEBUG")]
        public void CheckProcessIsMaster()
        {
            if (!IsMasterProcess) throw new MpiException(
                $"Process {OwnRank}: Only defined for master process (rank = {MasterProcess})");
        }

        [Conditional("DEBUG")]
        public void CheckProcessMatchesCluster(int clusterID)
        {
            if (clusterID != OwnRank) throw new MpiException(
                $"Process {OwnRank}: This process does not have access to cluster {clusterID}");
        }

        [Conditional("DEBUG")]
        public void CheckProcessMatchesClusterUnlessMaster(int clusterID)
        {
            if (IsMasterProcess) return;
            if (clusterID != OwnRank) throw new MpiException(
                $"Process {OwnRank}: This process does not have access to subdomain {clusterID}");
        }


        [Conditional("DEBUG")]
        public void CheckProcessMatchesSubdomain(int subdomainID)
        {
            bool isStored = processesToSubdomains[OwnRank].Contains(subdomainID);
            if (!isStored) throw new MpiException(
                $"Process {OwnRank}: This process does not have access to subdomain {subdomainID}");
        }

        [Conditional("DEBUG")]
        public void CheckProcessMatchesSubdomainUnlessMaster(int subdomainID)
        {
            if (IsMasterProcess) return;
            bool isStored = processesToSubdomains[OwnRank].Contains(subdomainID);
            if (!isStored) throw new MpiException(
                $"Process {OwnRank}: This process does not have access to subdomain {subdomainID}");
        }

        public int[] GetNumSubdomainsPerProcess()
        {
            var counts = new int[Communicator.Size];
            for (int p = 0; p < counts.Length; ++p) counts[p] = processesToSubdomains[p].Length;
            return counts;
        }

        public int GetSubdomainIdOfProcess(int processRank) => processesToSubdomains[processRank][0];

        /// <summary>
        /// The subdomain IDs are in the same order for all processes.
        /// </summary>
        /// <param name="processRank"></param>
        public int[] GetSubdomainIdsOfProcess(int processRank) => processesToSubdomains[processRank];

        public void PrintProcessDistribution()
        {
            if (IsMasterProcess)
            {
                Console.WriteLine("---------------------------------------");
                Console.WriteLine($"Printing from process {OwnRank}");
                for (int p = 0; p < Communicator.Size; ++p)
                {
                    var builder1 = new StringBuilder();
                    builder1.Append($"Process {p} - subdomain IDs: ");
                    foreach (int s in GetSubdomainIdsOfProcess(p))
                    {
                        builder1.Append(s + " ");
                    }
                    Console.WriteLine(builder1);
                }
            }

            if (IsMasterProcess)
            {
                Console.WriteLine("---------------------------------------");
                Console.WriteLine($"Printing from each process:");
            }
            Communicator.Barrier();
            var builder2 = new StringBuilder();
            builder2.Append($"Process {OwnRank} - subdomain IDs: ");
            foreach (int s in GetSubdomainIdsOfProcess(OwnRank))
            {
                builder2.Append(s + " ");
            }
            Console.WriteLine(builder2);
        }
    }
}
