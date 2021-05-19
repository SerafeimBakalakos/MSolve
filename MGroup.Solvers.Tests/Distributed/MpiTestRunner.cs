using System;
using System.Collections.Generic;
using System.Text;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.Environments;
using MGroup.Solvers_OLD.Tests.DistributedTry1.Distributed.LinearAlgebra;
using MPI;

//TODOMPI: Perhaps the XUnit.Assert() methods in the actual tests are not the best way for MPI. In this case polymorphism should 
//      be used for asserting and reporting the result.
namespace MGroup.Solvers_OLD.Tests.Distributed
{
    public static class MpiTestRunner
    {
        private static void AttachBareBonesDebugger()
        {
            var args = Array.Empty<String>();
            using (new MPI.Environment(ref args))
            {
                MpiUtilities.AssistDebuggerAttachment();

                Intracommunicator comm = Communicator.world;
                string header = $"Process {comm.Rank}: ";
                Console.WriteLine($"Process {comm.Rank}/{comm.Size - 1}: Hello world!");
            }
        }

        public static void Run(string[] args)
        {
            //AttachBareBonesDebugger();
            Hexagon1DTopologyTests.RunMpiTests();
            //Line1DTopologyTests.RunMpiTests();
        }

        
    }
}
