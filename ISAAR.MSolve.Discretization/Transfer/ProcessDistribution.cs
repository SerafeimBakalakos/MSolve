using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.Exceptions;
using ISAAR.MSolve.Discretization.Interfaces;
using MPI;

//TODO: What about the case where more than one subdomains are delegated to the same process?
//TODO: If there are point2point communications between two processes, should this be transfered to all of them?
namespace ISAAR.MSolve.Discretization.Transfer
{
    /// <summary>
    /// This should be present in all processes.
    /// </summary>
    public class ProcessDistribution
    {
        private readonly int[] processesToSubdomains;
        private readonly Dictionary<int, int> subdomainsToProcesses;

        public ProcessDistribution(Intracommunicator comm, int masterProcess, int[] processRanksToSubdomainIDs)
        {
            this.Communicator = comm;
            this.IsMasterProcess = comm.Rank == masterProcess;
            this.MasterProcess = masterProcess;
            this.OwnRank = comm.Rank;
            this.OwnSubdomainID = processRanksToSubdomainIDs[OwnRank];

            this.processesToSubdomains = processRanksToSubdomainIDs;
            this.subdomainsToProcesses = new Dictionary<int, int>();
            for (int p = 0; p < comm.Size; ++p) this.subdomainsToProcesses[processRanksToSubdomainIDs[p]] = p;
        }

        public Intracommunicator Communicator { get; }
        public bool IsMasterProcess { get; }
        public int MasterProcess { get; }
        public int OwnRank { get; }
        public int OwnSubdomainID { get; }

        [Conditional("DEBUG")]
        public void CheckProcessIsMaster()
        {
            if (!IsMasterProcess) throw new MpiException(
                $"Process {OwnRank}: Only defined for master process (rank = {MasterProcess})");
        }

        [Conditional("DEBUG")]
        public void CheckProcessMatchesSubdomain(int subdomainID)
        {
            if (subdomainID != OwnSubdomainID) throw new MpiException(
                $"Process {OwnRank}: This process does not have access to subdomain {subdomainID}");
        }

        [Conditional("DEBUG")]
        public void CheckProcessMatchesSubdomainUnlessMaster(int subdomainID)
        {
            if (IsMasterProcess) return;
            if (subdomainID != OwnSubdomainID) throw new MpiException(
                $"Process {OwnRank}: This process does not have access to subdomain {subdomainID}");
        }

        public int GetProcessOfSubdomain(int subdomainID) => subdomainsToProcesses[subdomainID];
        public int GetSubdomainIdOfProcess(int processRank) => processesToSubdomains[processRank];
    }
}
