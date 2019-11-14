using System;
using System.Collections.Generic;
using System.Text;

//TODO: This class uses the flattened versions of MPI calls. Add another one that uses the regular ones and benchmark.
//TODO: I should avoid packing/unpacking the subdomains of the master process. Also avoid placing them onto arrays.
namespace ISAAR.MSolve.Discretization.Transfer
{
    public class TransfererAltogetherFlattened : ISubdomainDataTransferer
    {
        private readonly ProcessDistribution procs;

        public TransfererAltogetherFlattened(ProcessDistribution processDistribution)
        {
            this.procs = processDistribution;
        }

        public Dictionary<int, T> GatherFromAllSubdomains<T>(Dictionary<int, T> processSubdomainsData)
            => GatherFromAllSubdomainsPacked<T, T>(processSubdomainsData, (s, data) => data, (s, data) => data);

        public Dictionary<int, TRaw> GatherFromAllSubdomainsPacked<TRaw, TPacked>(Dictionary<int, TRaw> processSubdomainsData, 
            PackSubdomainData<TRaw, TPacked> packData, UnpackSubdomainData<TRaw, TPacked> unpackData)
        {
            // Pack the subdomain data in each process
            int[] processSubdomains = procs.GetSubdomainIdsOfProcess(procs.OwnRank);
            var processDataPacked = new TPacked[processSubdomains.Length];
            for (int s = 0; s < processSubdomains.Length; ++s)
            {
                int sub = processSubdomains[s];
                TPacked packed = packData(sub, processSubdomainsData[sub]);
                processDataPacked[s] = packed;
            }

            // Gather all subdomain data in master
            int[] numSubdomainsPerProcess = procs.GetNumSubdomainsPerProcess();
            TPacked[] allDataPacked = procs.Communicator.GatherFlattened<TPacked>(
                processDataPacked, numSubdomainsPerProcess, procs.MasterProcess);

            // Unpack all subdomain data in master
            Dictionary<int, TRaw> allData = null;
            int offset = 0;
            if (procs.IsMasterProcess)
            {
                allData = new Dictionary<int, TRaw>();
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    foreach (int sub in procs.GetSubdomainIdsOfProcess(p))
                    {
                        TPacked packed = allDataPacked[offset];
                        allData[sub] = unpackData(sub, packed);
                        allDataPacked[offset] = default; // Free up some memory
                        ++offset;
                    }
                }
                
            }
            return allData;
        }

        public Dictionary<int, TRaw> GatherFromAllSubdomainsPacked<TRaw, TPacked>(Dictionary<int, TRaw> processSubdomainsData, 
            GetArrayLengthOfPackedData<TRaw> getPackedDataLength, PackSubdomainDataIntoArray<TRaw, TPacked> packData, 
            UnpackSubdomainDataFromArray<TRaw, TPacked> unpackData)
        {
            // Determine the array sizes in each process
            int[] processSubdomains = procs.GetSubdomainIdsOfProcess(procs.OwnRank);
            int processCount = 0;
            var processOffsets = new int[processSubdomains.Length];
            for (int s = 0; s < processSubdomains.Length; ++s)
            {
                int sub = processSubdomains[s];
                TRaw rawData = processSubdomainsData[sub];
                int packedLength = getPackedDataLength(sub, rawData);
                if (s < processSubdomains.Length - 1) processOffsets[s + 1] = processOffsets[s] + packedLength;
                processCount += packedLength;
            }

            // Determine the array sizes in master
            int[] allCounts = procs.Communicator.Gather<int>(processCount, procs.MasterProcess);

            // Pack the subdomain data in each process
            var processDataPacked = new TPacked[processCount];
            for (int s = 0; s < processSubdomains.Length; ++s)
            {
                int sub = processSubdomains[s];
                packData(sub, processSubdomainsData[sub], processDataPacked, processOffsets[s]);
            }

            // Gather all subdomain data in master
            TPacked[] allDataPacked = procs.Communicator.GatherFlattened<TPacked>(
                processDataPacked, allCounts, procs.MasterProcess);

            // Unpack all subdomain data in master
            Dictionary<int, TRaw> allData = null;
            int offset = 0;
            if (procs.IsMasterProcess)
            {
                allData = new Dictionary<int, TRaw>();
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    foreach (int sub in procs.GetSubdomainIdsOfProcess(p))
                    {
                        TPacked packed = allDataPacked[offset];
                        TRaw raw = unpackData(sub, allDataPacked, offset);
                        allData[sub] = raw;
                        offset += getPackedDataLength(sub, raw);
                    }
                }
            }
            return allData;
        }

        public Dictionary<int, T> GatherFromSomeSubdomains<T>(Dictionary<int, T> processSubdomainsData, 
            ActiveSubdomains activeSubdomains)
        {
            return GatherFromSomeSubdomainsPacked<T, T>(processSubdomainsData, (s, data) => data, (s, data) => data,
                activeSubdomains);
        }

        public Dictionary<int, TRaw> GatherFromSomeSubdomainsPacked<TRaw, TPacked>(Dictionary<int, TRaw> processSubdomainsData, 
            PackSubdomainData<TRaw, TPacked> packData, UnpackSubdomainData<TRaw, TPacked> unpackData, 
            ActiveSubdomains activeSubdomains)
        {
            throw new NotImplementedException();
        }

        public Dictionary<int, TRaw> GatherFromSomeSubdomainsPacked<TRaw, TPacked>(Dictionary<int, TRaw> processSubdomainsData, 
            GetArrayLengthOfPackedData<TRaw> getPackedDataLength, PackSubdomainDataIntoArray<TRaw, TPacked> packData, 
            UnpackSubdomainDataFromArray<TRaw, TPacked> unpackData, ActiveSubdomains activeSubdomains)
        {
            throw new NotImplementedException();
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
                        TPacked packed = packData(sub, allSubdomainsData_master[sub]);
                        allDataPacked[offset++] = packed;
                    }
                }
            }

            // Scatter the global array
            int[] numSubdomainsPerProcess = procs.GetNumSubdomainsPerProcess();
            TPacked[] processDataPacked = procs.Communicator.ScatterFromFlattened(
                allDataPacked, numSubdomainsPerProcess, procs.MasterProcess);

            // Unpack the subdomain data of this process
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

        public Dictionary<int, TRaw> ScatterToAllSubdomainsPacked<TRaw, TPacked>(Dictionary<int, TRaw> allSubdomainsData_master, 
            GetArrayLengthOfPackedData<TRaw> getPackedDataLength, PackSubdomainDataIntoArray<TRaw, TPacked> packData, 
            UnpackSubdomainDataFromArray<TRaw, TPacked> unpackData)
        {
            // Determine the array sizes in master
            TPacked[] allDataPacked = null;
            var processCounts = new int[procs.Communicator.Size];
            if (procs.IsMasterProcess)
            {
                int totalSize = 0;
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    foreach (int sub in procs.GetSubdomainIdsOfProcess(p))
                    {
                        TRaw rawData = allSubdomainsData_master[sub];
                        int packedLength = getPackedDataLength(sub, rawData);
                        totalSize += packedLength;
                        processCounts[p] += packedLength;
                    }
                }
                allDataPacked = new TPacked[totalSize];
            }

            // Determine the array sizes in other processes. //TODO: Why is this necessary?
            procs.Communicator.Broadcast<int>(ref processCounts, procs.MasterProcess);

            // Put all data in a global array in master
            if (procs.IsMasterProcess)
            {
                int offset = 0;
                for (int p = 0; p < procs.Communicator.Size; ++p)
                {
                    foreach (int sub in procs.GetSubdomainIdsOfProcess(p))
                    {
                        TRaw rawData = allSubdomainsData_master[sub];
                        packData(sub, rawData, allDataPacked, offset);
                        offset += getPackedDataLength(sub, rawData);
                    }
                }
            }

            // Scatter the global array
            TPacked[] processDataPacked = procs.Communicator.ScatterFromFlattened(
                allDataPacked, processCounts, procs.MasterProcess);

            // Unpack the subdomain data of this process
            Dictionary<int, TRaw> processData = null;
            if (!procs.IsMasterProcess)
            {
                int[] subdomainsOfProcess = procs.GetSubdomainIdsOfProcess(procs.OwnRank);
                processData = new Dictionary<int, TRaw>(subdomainsOfProcess.Length);
                int offset = 0;
                for (int s = 0; s < subdomainsOfProcess.Length; ++s)
                {
                    int sub = subdomainsOfProcess[s];
                    processData[sub] = unpackData(sub, processDataPacked, offset);
                    offset += getPackedDataLength(sub, processData[sub]);
                }
            }
            return processData;
        }

        public Dictionary<int, T> ScatterToSomeSubdomains<T>(Dictionary<int, T> allSubdomainsData_master, 
            ActiveSubdomains activeSubdomains)
        {
            return ScatterToSomeSubdomainsPacked<T, T>(allSubdomainsData_master, (s, data) => data, (s, data) => data,
                activeSubdomains);
        }

        public Dictionary<int, TRaw> ScatterToSomeSubdomainsPacked<TRaw, TPacked>(Dictionary<int, TRaw> allSubdomainsData_master,
            PackSubdomainData<TRaw, TPacked> packData, UnpackSubdomainData<TRaw, TPacked> unpackData, 
            ActiveSubdomains activeSubdomains)
        {
            throw new NotImplementedException();
        }

        public Dictionary<int, TRaw> ScatterToSomeSubdomainsPacked<TRaw, TPacked>(Dictionary<int, TRaw> allSubdomainsData_master, 
            GetArrayLengthOfPackedData<TRaw> getPackedDataLength, PackSubdomainDataIntoArray<TRaw, TPacked> packData, 
            UnpackSubdomainDataFromArray<TRaw, TPacked> unpackData, ActiveSubdomains activeSubdomains)
        {
            throw new NotImplementedException();
        }
    }
}
