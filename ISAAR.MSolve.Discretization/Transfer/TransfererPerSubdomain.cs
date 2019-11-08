using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;

namespace ISAAR.MSolve.Discretization.Transfer
{
    public class TransfererPerSubdomain : ISubdomainDataTransferer
    {
        private readonly ProcessDistribution procs;

        public TransfererPerSubdomain(ProcessDistribution processDistribution)
        {
            this.procs = processDistribution;
        }

        public Dictionary<int, T> ScatterToAllSubdomains<T>(Dictionary<int, T> allSubdomainsData_master)
            => ScatterToAllSubdomainsPacked<T, T>(allSubdomainsData_master, (s, data) => data, (s, data) => data);

        /// <summary>
        /// This method returns null in master process. For other processes, it returns a Dictionary with the data for each 
        /// associated subdomain.
        /// </summary>
        public Dictionary<int, TRaw> ScatterToAllSubdomainsPacked<TRaw, TPacked>(
            Dictionary<int, TRaw> allSubdomainsData_master,
            PackSubdomainData<TRaw, TPacked> packData, UnpackSubdomainData<TRaw, TPacked> unpackData)
        {
            // Pack and send the subdomain data to the corresponding process, one subdomain at a time
            if (procs.IsMasterProcess)
            {
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    if (p == procs.MasterProcess) continue;
                    else
                    {
                        foreach (int s in procs.GetSubdomainIdsOfProcess(p))
                        {
                            TPacked dto = packData(s, allSubdomainsData_master[s]);
                            procs.Communicator.Send<TPacked>(dto, p, s);
                        }
                    }
                }
                return null;
            }
            else
            {
                // At first, receive all subdomain data of this process, so that master can continue to the next process.
                int[] processSubdomains = procs.GetSubdomainIdsOfProcess(procs.OwnRank);
                var processDataPacked = new Dictionary<int, TPacked>(processSubdomains.Length);
                foreach (int s in processSubdomains)
                {
                    processDataPacked[s] = procs.Communicator.Receive<TPacked>(procs.MasterProcess, s);
                }

                // Then unpack and return the subdomain data in each process
                var processData = new Dictionary<int, TRaw>(processSubdomains.Length);
                foreach (int s in processSubdomains)
                {
                    processData[s] = unpackData(s, processDataPacked[s]);
                }
                return processData;
            }
        }
    }
}
