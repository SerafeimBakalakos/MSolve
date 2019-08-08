using System;
using System.Collections.Generic;
using System.Text;
using MPI;

//TODO: Ideally these would be strategy objects
namespace ISAAR.MSolve.Solvers.DomainDecomposition
{
    public static class MpiUtilities
    {
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
    }
}
