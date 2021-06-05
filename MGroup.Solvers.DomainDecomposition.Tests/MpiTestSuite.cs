﻿using System;
using System.Collections.Generic;
using System.Text;
using MGroup.Environments.Mpi;
using MGroup.Solvers.DomainDecomposition.Tests.PSM;

//TODO: Move this to the same project as MPI environment
//TODO: Allow client to register tests to be run, schedule them, run them with appropriate before and after messages, 
//      handle exceptions by adequately notifying user, but without crushing and omitting the next tests. 
//TODO: Hande the case of 1 machine running multiple tests that need different number of processes (make sure the max needed 
//      have been launched, deactivate the extras per test so that they do not mess with collectives). 
//      Handle the case of multiple machines running tests. Facilite user with command line parameters to MPI exec
namespace MGroup.Solvers.DomainDecomposition.Tests
{
    public static class MpiTestSuite
    {
        public static void RunTestsWith4Processes()
        {
            using (var mpiEnvironment = new MpiEnvironment())
            {
                MpiDebugUtilities.AssistDebuggerAttachment();

                MpiDebugUtilities.DoSerially(MPI.Communicator.world,
                    () => Console.WriteLine(
                        $"Process {MPI.Communicator.world.Rank}: Now running DofSeparatorTests.TestInLine1DExample"));
                PsmDofSeparatorTests.TestInLine1DExample(mpiEnvironment);

                MpiDebugUtilities.DoSerially(MPI.Communicator.world,
                    () => Console.WriteLine(
                        $"Process {MPI.Communicator.world.Rank}: Now running DofSeparatorTests.TestInPlane2DExample"));
                PsmDofSeparatorTests.TestInPlane2DExample(mpiEnvironment);

                MpiDebugUtilities.DoSerially(MPI.Communicator.world,
                    () => Console.WriteLine(
                        $"Process {MPI.Communicator.world.Rank}: Now running PsmSolverTests.TestInLine1DExample"));
                PsmSolverTests.TestInLine1DExample(mpiEnvironment);

                MpiDebugUtilities.DoSerially(MPI.Communicator.world,
                    () => Console.WriteLine($"Process {MPI.Communicator.world.Rank}: All tests passed"));
            }
        }
    }
}
