using System;
using System.Collections.Generic;
using System.Text;
using MGroup.Solvers.Tests.Distributed.LinearAlgebra;
using MPI;

namespace MGroup.Solvers.Tests.Distributed
{
    public static class MpiTestRunner
    {
        public static void Run(string[] args)
        {
            //DistributedOverlappingVectorTestsMpi.TestAxpy();
            //DistributedOverlappingVectorTestsMpi.TestDotProduct();
            //DistributedOverlappingVectorTestsMpi.TestEquals();
            DistributedOverlappingVectorTestsMpi.TestRhsVectorConvertion();

            //using (new MPI.Environment(ref args))
            //{
            //    MpiUtilities.AssistDebuggerAttachment();

            //    Intracommunicator comm = Communicator.world;
            //    string header = $"Process {comm.Rank}: ";
            //    Console.WriteLine($"Process {comm.Rank}/{comm.Size - 1}: Hello world!");
            //}
        }
    }
}
