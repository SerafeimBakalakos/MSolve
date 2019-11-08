using System;
using System.Collections.Generic;
using System.Text;

//TODO: This class uses the flattened versions of MPI calls. Add another one that uses the regular ones and benchmark.
namespace ISAAR.MSolve.Discretization.Transfer
{
    public class TransfererAltogetherFlattened : ISubdomainDataTransferer
    {
        private readonly ProcessDistribution procs;

        public TransfererAltogetherFlattened(ProcessDistribution processDistribution)
        {
            this.procs = processDistribution;
        }

        public Dictionary<int, T> ScatterToAllSubdomains<T>(Dictionary<int, T> allSubdomainsData_master)
            => ScatterToAllSubdomainsPacked<T, T>(allSubdomainsData_master, (s, data) => data, (s, data) => data);

        public Dictionary<int, TRaw> ScatterToAllSubdomainsPacked<TRaw, TPacked>(Dictionary<int, TRaw> allSubdomainsData_master, 
            PackSubdomainData<TRaw, TPacked> packData, UnpackSubdomainData<TRaw, TPacked> unpackData)
        {
            // Put all data in a global array
            TPacked[] allDataPacked = null;
            if (procs.IsMasterProcess)
            {
                allDataPacked = new TPacked[procs.NumSubdomainsTotal];
                int offset = 0;
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    foreach (int sub in procs.GetSubdomainIdsOfProcess(p))
                    {
                        TPacked dto = packData(sub, allSubdomainsData_master[sub]);
                        allDataPacked[offset++] = dto;
                    }
                }
            }

            // Scatter the global array
            int[] numSubdomainsPerProcess = procs.GetNumSubdomainsPerProcess();
            TPacked[] processDataPacked = procs.Communicator.ScatterFromFlattened(
                allDataPacked, numSubdomainsPerProcess, procs.MasterProcess);

            // Output the subdomain data of this process
            Dictionary<int, TRaw> processData = null;
            if (!procs.IsMasterProcess)
            {
                int[] subdomainsOfProcess = procs.GetSubdomainIdsOfProcess(procs.OwnRank);
                processData = new Dictionary<int, TRaw>(subdomainsOfProcess.Length);
                for (int s = 0; s < subdomainsOfProcess.Length; ++s)
                {
                    int sub = subdomainsOfProcess[s];
                    processData[sub] = unpackData(sub, processDataPacked[s]);
                }
            }
            return processData;
        }
    }
}
