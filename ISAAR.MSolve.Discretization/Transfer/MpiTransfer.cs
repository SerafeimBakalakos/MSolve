using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using MPI;

namespace ISAAR.MSolve.Discretization.Transfer
{
    public class MpiTransfer
    {
        private readonly ISubdomainSerializer serializer;

        public MpiTransfer(ISubdomainSerializer serializer)
        {
            this.serializer = serializer;
        }

        /// <summary>
        /// Given the model data in one process, scatters the subdomain data to all processes. This method is meant to be called 
        /// by all MPI processes.
        /// </summary>
        /// <param name="model">
        /// It can be null for processes other than the root. model.ConnectDataStructures() must have been called beforehand.
        /// </param>
        /// <param name="root">Root process where the data of <paramref name="model"/> are stored.</param>
        public ISubdomain ScatterSubdomains(IModel model, int root)
        {
            Intracommunicator comm = Communicator.world;
            int rank = comm.Rank;
            //Console.WriteLine($"(process {rank}) Hello World!"); // Run this to check if MPI works correctly.

            // Serialize the data of each subdomain
            IReadOnlyList<ISubdomain> originalSubdomains = null;
            ISubdomainDto[] serializedSubdomains = null;
            if (rank == root)
            {
                int numSubdomains = model.Subdomains.Count;
                originalSubdomains = model.Subdomains;
                serializedSubdomains = new ISubdomainDto[numSubdomains];
                serializedSubdomains[0] = new EmptySubdomainDto();
                for (int s = 1; s < numSubdomains; ++s)
                {
                    serializedSubdomains[s] = serializer.Serialize(originalSubdomains[s]);
                }
            }

            // Scatter the serialized subdomain data from master process and deserialize in each process
            ISubdomainDto serializedSubdomain = comm.Scatter(serializedSubdomains, root);
            if (rank == root) return originalSubdomains[0];
            else return serializedSubdomain.Deserialize();
        }
    }
}
