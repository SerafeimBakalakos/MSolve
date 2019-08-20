using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using MPI;

namespace ISAAR.MSolve.Discretization.Transfer.Utilities
{
    public class DofTableTransfer
    {
        private readonly Intracommunicator comm;
        private readonly IDofSerializer dofSerializer;
        private readonly int master;
        private readonly ProcessDistribution processDistribution;

        private IEnumerable<ISubdomain> subdomainsToGatherFrom_master;
        private Dictionary<int, INode> globalNodes_master;
        private bool gatherFromThisSubdomain_slave;
        private DofTable dofsToSend_slave;

        public DofTableTransfer(Intracommunicator comm, int master, ProcessDistribution processDistribution, 
            IDofSerializer dofSerializer)
        {
            this.comm = comm;
        }

        public Dictionary<int, int> NumSubdomainDofs_master { get; }
        public Dictionary<int, DofTable> SubdomainDofOrderings_master { get; }

        public void DefineModelData_master(IEnumerable<ISubdomain> subdomainsToGatherFrom, Dictionary<int, INode> globalNodes)
        {
            this.globalNodes_master = globalNodes;

            // Single out the subdomain of the master process, since no MPI transfers are required for it.
            ISubdomain masterSubdomain = processDistribution.ProcesesToSubdomains[master];
            this.subdomainsToGatherFrom_master = subdomainsToGatherFrom.Where(sub => sub.ID != masterSubdomain.ID);
        }

        public void DefineSubdomainData_slave(bool gatherFromThisSubdomain, DofTable dofsToGather)
        {
            this.gatherFromThisSubdomain_slave = gatherFromThisSubdomain;
            this.dofsToSend_slave = dofsToGather;
        }

        public void Transfer(int mpiTag)
        {
            var tableSerializer = new DofTableSerializer(dofSerializer);

            // Receive the corner dof ordering of each subdomain from the corresponding process
            if (comm.Rank == master)
            {
                var serializedTables = new Dictionary<ISubdomain, int[]>();
                foreach (ISubdomain subdomain in subdomainsToGatherFrom_master)
                {
                    // Receive the corner dof ordering of each subdomain
                    int source = processDistribution.SubdomainsToProcesses[subdomain];
                    //Console.WriteLine($"Process {comm.Rank} (master): Started receiving dof ordering from process {source}.");
                    serializedTables[subdomain] = MpiUtilities.ReceiveArray<int>(comm, source, mpiTag);
                    //Console.WriteLine($"Process {comm.Rank} (master): Finished receiving dof ordering from process {source}.");
                }

                // After finishing with all comunications deserialize the received items. //TODO: This should be done concurrently with the transfers, by another thread.
                foreach (ISubdomain subdomain in subdomainsToGatherFrom_master)
                {
                    bool isModified = serializedTables.TryGetValue(subdomain, out int[] serializedTable);
                    if (isModified)
                    {
                        //Console.WriteLine($"Process {comm.Rank} (master): Started deserializing corner dof ordering of subdomain {subdomain.ID}.");
                        NumSubdomainDofs_master[subdomain.ID] = tableSerializer.CountEntriesOf(serializedTable);
                        SubdomainDofOrderings_master[subdomain.ID] = 
                            tableSerializer.Deserialize(serializedTable, globalNodes_master);
                        //Console.WriteLine($"Process {comm.Rank} (master): Finished deserializing corner dof ordering of subdomain {subdomain.ID}.");
                        serializedTables.Remove(subdomain); // Free up some temporary memory.
                    }
                }
            }
            else
            {
                if (gatherFromThisSubdomain_slave)
                {
                    //Console.WriteLine($"Process {rank}: Started serializing dof ordering.");
                    int[] serializedTable = tableSerializer.Serialize(dofsToSend_slave);
                    //Console.WriteLine($"Process {rank}: Finished serializing dof ordering. Started sending it to master");
                    MpiUtilities.SendArray(comm, serializedTable, master, mpiTag);
                    //Console.WriteLine($"Process {rank}: Finished sending corner dof ordering to master.");
                }
            }
        }
    }
}
