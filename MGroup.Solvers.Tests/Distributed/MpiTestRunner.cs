using System;
using System.Collections.Generic;
using System.Text;
using MPI;

namespace MGroup.Solvers.Tests.Distributed
{
    public static class MpiTestRunner
    {
        public static void Run(string[] args)
        {
            using (new MPI.Environment(ref args))
            {
                Intracommunicator comm = Communicator.world;
                string header = $"Process {comm.Rank}: ";

                Console.WriteLine($"Process {comm.Rank}/{comm.Size - 1}: Hello world!");
            }
        }
    }
}
