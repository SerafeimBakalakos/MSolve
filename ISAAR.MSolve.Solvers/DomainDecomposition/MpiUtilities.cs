using System;
using System.Collections.Generic;
using System.Text;
using MPI;

//TODO: Ideally these would be strategy objects
namespace ISAAR.MSolve.Solvers.DomainDecomposition
{
    public static class MpiUtilities
    {
        public static void BroadcastArray(Intracommunicator comm, ref int[] values, int root, int tag)
        {
            // First broadcast the length of the array
            int rank = comm.Rank;
            int length = -1;
            if (rank == root) length = values.Length;
            comm.Broadcast<int>(ref length, root);

            // Allocate buffers in receiving processes
            if (rank != root) values = new int[length];

            //The broadcast the whole array
            comm.Broadcast<int>(ref values, root);
        }

        public static T[] GatherFromSubdomains<T>(Intracommunicator comm, T subdomainData, int masterProcess)
        {
            return comm.Gather(subdomainData, masterProcess);
        }

        //public static T[] GatherFromSubdomains<T>(Intracommunicator comm, T subdomainData, int masterProcess)
        //{
        //Dictionary<int, DofTable> subdomainCornerDofOrderings = null;
        //var tableSerializer = new DofTableSerializer(dofSerializer);
        //if (rank == masterProcess)
        //{
        //    subdomainCornerDofOrderings = new Dictionary<int, DofTable>();
        //    subdomainCornerDofOrderings[masterProcess] = SubdomainDofs.CornerDofOrdering;
        //    var requests = new Dictionary<int, ReceiveRequest>();
        //    var pending = new HashSet<int>();
        //    for (int p = 0; p < comm.Size; ++p)
        //    {
        //        if (p == masterProcess) continue;
        //        requests[p] = comm.ImmediateReceive<int[]>(p, cornerDofOrderingTag);
        //        pending.Add(p);
        //    }

        //    while (pending.Count > 0) // perhaps the thread should sleep, until a request is completed
        //    {
        //        foreach (int p in pending)
        //        {
        //            CompletedStatus status = requests[p].Test();
        //            if (status != null)
        //            {

        //            }
        //        }
        //    }

        //    for (int p = 0; p < comm.Size; ++p)
        //    {
        //        if (p == masterProcess) continue;
        //        int[] flatTable = requests[p].Get
        //    }
        //}
        //else
        //{

        //    comm.ImmediateSend(SubdomainDofs.CornerDofOrdering)
        //}
        //}

        /// <summary>
        /// Must be paired with <see cref="SendArray(Intracommunicator, int[], int, int)(Intracommunicator, int, int)"/>.
        /// </summary>
        /// <param name="comm"></param>
        /// <param name="source"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static int[] ReceiveArray(Intracommunicator comm, int source, int tag)
        {
            // Receiving the length is needed to allocate a buffer before receiving the whole array. Furthermore it 
            // blocks all processes except for the one currently processed master, preventing them from sending the whole 
            // arrays. Not sure if this is good or bad.
            int length = comm.Receive<int>(source, tag);
            var buffer = new int[length];
            comm.Receive(source, tag, ref buffer);
            return buffer;
        }

        /// <summary>
        /// Must be paired with <see cref="ReceiveArray(Intracommunicator, int, int)"/>.
        /// </summary>
        /// <param name="comm"></param>
        /// <param name="vals"></param>
        /// <param name="dest"></param>
        /// <param name="tag"></param>
        public static void SendArray(Intracommunicator comm, int[] vals, int dest, int tag)
        {
            // Sending the length is needed to allocate a buffer in destination before sending the whole array. Furthermore it 
            // blocks all processes except for the one currently processed master, preventing them from sending the whole 
            // arrays. Not sure if this is good or bad.
            comm.Send(vals.Length, dest, tag);
            comm.Send(vals, dest, tag);
        }
    }
}
